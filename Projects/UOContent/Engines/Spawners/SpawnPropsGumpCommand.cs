/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SpawnPropsGump.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Targeting;

namespace Server.Engines.Spawners
{
    public class SpawnPropsGumpCommand : BaseCommand
    {
        public static void Initialize()
        {
            TargetCommands.Register(new SpawnPropsGumpCommand());
        }

        public SpawnPropsGumpCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Complex | CommandSupport.Simple;
            Commands = new[] { "SpawnProps" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "SpawnProps <type>";
            Description = "Shows a props gump that will modify the properties of spawn entries related to the chosen type";
            ListOptimized = true;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (list.Count == 0)
            {
                LogFailure("No matching objects found.");
                return;
            }

            var args = e.Arguments;

            if (args.Length == 0)
            {
                e.Mobile.Target =new InternalTarget(list);
                return;
            }

            var name = args[0];

            var type = AssemblyHandler.FindTypeByName(name);

            if (!Add.IsEntity(type))
            {
                LogFailure("No type with that name was found.");
                return;
            }

            e.Mobile.SendGump(new SpawnPropsGump(e.Mobile, type, list));
        }

        private class InternalTarget : Target
        {
            private List<object> _list;

            public InternalTarget(List<object> list) : base(-1, false, TargetFlags.None) =>
                _list = list;

            protected override void OnTarget(Mobile from, object targeted)
            {
                var type = targeted.GetType();
                if (!Add.IsEntity(type))
                {
                    from.SendMessage("No type with that name was found.");
                }

                from.SendGump(new SpawnPropsGump(from, type, _list));
            }
        }
    }
}
