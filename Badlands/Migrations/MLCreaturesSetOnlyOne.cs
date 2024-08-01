// Copyright (C) 2024 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Server;
using Server.Engines.Spawners;
using Server.Mobiles;

namespace Badlands.Migrations;

public class MLCreaturesSetOnlyOne : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var items = new List<Serial>();

        var types = new[]
        {
            nameof( Virulent ), nameof( Silk ), nameof( Irk ), nameof( Spite ), nameof( Malefic ), nameof( Guile ),
            nameof( Gnaw ), nameof( Abscess ), nameof( Saliva ), nameof( Tangle ), nameof( Putrefier )
        };

        var spawners = World.Items.Values.Where(
            e => e is BaseSpawner spawner && spawner.Entries.Any( e => types.Contains( e.SpawnedName ) )
        );

        foreach ( var item in spawners.ToList() )
        {
            if ( item is BaseSpawner spawner )
            {
                var entries = spawner.Entries.Where( e => types.Contains( e.SpawnedName ) );

                foreach ( var entry in entries )
                {
                    entry.SpawnedMaxCount = 1;
                }

                spawner.Respawn();
            }

            items.Add( item.Serial );
        }

        return items;
    }

    public void Down()
    {
    }
}
