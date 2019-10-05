using System.Collections.Generic;
using Server.Gumps;
using Server.Multis;
using Server.Prompts;
using Server.Mobiles;
using Server.ContextMenus;
using Server.Items;

namespace Server.Engines.BulkOrders
{
	public class BulkOrderBook : Item, ISecurable
	{
		private string m_BookName;

		[CommandProperty( AccessLevel.GameMaster )]
		public string BookName
		{
			get => m_BookName;
			set{ m_BookName = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public SecureLevel Level { get; set; }

		public List<IBOBEntry> Entries { get; private set; }

		public BOBFilter Filter { get; private set; }

		public int ItemCount { get; set; }

		[Constructible]
		public BulkOrderBook() : base( 0x2259 )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;

			Entries = new List<IBOBEntry>();
			Filter = new BOBFilter();

			Level = SecureLevel.CoOwners;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
				from.LocalOverheadMessage( Network.MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
			else if ( Entries.Count == 0 )
				from.SendLocalizedMessage( 1062381 ); // The book is empty.
			else if ( from is PlayerMobile mobile )
				mobile.SendGump( new BOBGump( mobile, this ) );
		}

		public override void OnDoubleClickSecureTrade( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.SendLocalizedMessage( 500446 ); // That is too far away.
			}
			else if ( Entries.Count == 0 )
			{
				from.SendLocalizedMessage( 1062381 ); // The book is empty.
			}
			else
			{
				from.SendGump( new BOBGump( (PlayerMobile)from, this ) );

        SecureTrade trade = GetSecureTradeCont()?.Trade;

        if (trade?.From.Mobile == from )
          trade.To.Mobile.SendGump( new BOBGump( (PlayerMobile)trade.To.Mobile, this ) );
        else if (trade?.To.Mobile == from )
          trade.From.Mobile.SendGump( new BOBGump( (PlayerMobile)trade.From.Mobile, this ) );
			}
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( dropped is BaseBOD )
			{
				if ( !IsChildOf( from.Backpack ) )
				{
					from.SendLocalizedMessage( 1062385 ); // You must have the book in your backpack to add deeds to it.
					return false;
				}
				if ( !from.Backpack.CheckHold( from, dropped, true, true ) )
					return false;
				if ( Entries.Count < 500 )
				{
					if ( dropped is LargeBOD bod )
						Entries.Add( new BOBLargeEntry( bod ) );
					else
						Entries.Add( new BOBSmallEntry( (SmallBOD)dropped ) );

					InvalidateProperties();

					if ( Entries.Count / 5 > ItemCount )
					{
						ItemCount++;
						InvalidateItems();
					}

					from.SendSound(0x42, GetWorldLocation());
					from.SendLocalizedMessage( 1062386 ); // Deed added to book.

					if ( from is PlayerMobile pm )
						pm.SendGump( new BOBGump( pm, this ) );

					dropped.Delete();

					return true;
				}

				from.SendLocalizedMessage( 1062387 ); // The book is full of deeds.
				return false;
			}

			from.SendLocalizedMessage( 1062388 ); // That is not a bulk order deed.
			return false;
		}

		public override int GetTotal( TotalType type )
		{
			int total = base.GetTotal( type );

			if ( type == TotalType.Items )
				total = ItemCount;

			return total;
		}

		public void InvalidateItems()
		{
			if ( RootParent is Mobile m )
			{
				m.UpdateTotals();
				InvalidateContainers( Parent );
			}
		}

		public void InvalidateContainers(IEntity parent)
		{
			if ( parent is Container c )
			{
				c.InvalidateProperties();
				InvalidateContainers( c.Parent );
			}
		}

		public BulkOrderBook( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( 2 ); // version

			writer.Write( ItemCount );

			writer.Write( (int) Level );

			writer.Write( m_BookName );

			Filter.Serialize( writer );

			writer.WriteEncodedInt( Entries.Count );

			for ( int i = 0; i < Entries.Count; ++i )
			{
				object obj = Entries[i];

				if ( obj is BOBLargeEntry entry )
				{
					writer.WriteEncodedInt( 0 );
					entry.Serialize( writer );
				}
				else
				{
					writer.WriteEncodedInt( 1 );
					((BOBSmallEntry)obj).Serialize( writer );
				}
			}
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 2:
				{
					ItemCount = reader.ReadInt();
					goto case 1;
				}
				case 1:
				{
					Level = (SecureLevel)reader.ReadInt();
					goto case 0;
				}
				case 0:
				{
					m_BookName = reader.ReadString();

					Filter = new BOBFilter( reader );

					int count = reader.ReadEncodedInt();

					Entries = new List<IBOBEntry>( count );

					for ( int i = 0; i < count; ++i )
					{
						int v = reader.ReadEncodedInt();

						switch ( v )
						{
							case 0: Entries.Add( new BOBLargeEntry( reader ) ); break;
							case 1: Entries.Add( new BOBSmallEntry( reader ) ); break;
						}
					}

					break;
				}
			}
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1062344, Entries.Count.ToString() ); // Deeds in book: ~1_val~

			if ( !string.IsNullOrEmpty(m_BookName) )
				list.Add( 1062481, m_BookName ); // Book Name: ~1_val~
		}

		public override void OnSingleClick(Mobile from)
		{
			base.OnSingleClick(from);

			LabelTo(from, 1062344, Entries.Count.ToString()); // Deeds in book: ~1_val~

			if (!string.IsNullOrEmpty(m_BookName))
				LabelTo(from, 1062481, m_BookName);
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( from.CheckAlive() && IsChildOf( from.Backpack ) )
				list.Add( new NameBookEntry( from, this ) );

			SetSecureLevelEntry.AddTo( from, this, list );
		}

		private class NameBookEntry : ContextMenuEntry
		{
			private Mobile m_From;
			private BulkOrderBook m_Book;

			public NameBookEntry( Mobile from, BulkOrderBook book ) : base( 6216 )
			{
				m_From = from;
				m_Book = book;
			}

			public override void OnClick()
			{
				if ( m_From.CheckAlive() && m_Book.IsChildOf( m_From.Backpack ) )
				{
					m_From.Prompt = new NameBookPrompt( m_Book );
					m_From.SendLocalizedMessage( 1062479 ); // Type in the new name of the book:
				}
			}
		}

		private class NameBookPrompt : Prompt
		{
			private BulkOrderBook m_Book;

			public NameBookPrompt( BulkOrderBook book ) => m_Book = book;

      public override void OnResponse( Mobile from, string text )
			{
				if ( text.Length > 40 )
					text = text.Substring( 0, 40 );

				if ( from.CheckAlive() && m_Book.IsChildOf( from.Backpack ) )
				{
					m_Book.BookName = Utility.FixHtml( text.Trim() );

					from.SendLocalizedMessage( 1062480 ); // The bulk order book's name has been changed.
				}
			}

			public override void OnCancel( Mobile from )
			{
			}
		}
	}
}
