using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Server
{
  public class NameList
  {
    public string Type { get; }

    public string[] List { get; }

    public bool ContainsName( string name ) => List.Any(t => name == t);

    public NameList( string type, XmlElement xml )
    {
      Type = type;
      List = xml.InnerText.Split( ',' );

      for ( int i = 0; i < List.Length; ++i )
        List[i] = Utility.Intern( List[i].Trim() );
    }

    public string GetRandomName() => List.Length > 0 ? List[Utility.Random( List.Length )] : "";

    public static NameList GetNameList( string type )
    {
      m_Table.TryGetValue( type, out NameList n );
      return n;
    }

    public static string RandomName( string type ) => GetNameList( type )?.GetRandomName() ?? "";

    private static Dictionary<string, NameList> m_Table;

    static NameList()
    {
      m_Table = new Dictionary<string, NameList>( StringComparer.OrdinalIgnoreCase );

      string filePath = Path.Combine( Core.BaseDirectory, "Data/names.xml" );

      if ( !File.Exists( filePath ) )
        return;

      try
      {
        Load( filePath );
      }
      catch ( Exception e )
      {
        Console.WriteLine( "Warning: Exception caught loading name lists:" );
        Console.WriteLine( e );
      }
    }

    private static void Load( string filePath )
    {
      XmlDocument doc = new XmlDocument();
      doc.Load( filePath );

      XmlElement root = doc["names"];

      foreach ( XmlElement element in root.GetElementsByTagName( "namelist" ) )
      {
        string type = element.GetAttribute( "type" );

        if ( string.IsNullOrEmpty( type ) )
          continue;

        try
        {
          NameList list = new NameList( type, element );

          m_Table[type] = list;
        }
        catch
        {
          // ignored
        }
      }
    }
  }
}
