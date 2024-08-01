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

namespace Badlands.Data.Citadel;

public class CitadelTeleporterEntry
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public int Map { get; set; }
    public int ID { get; set; }
    public int Hue { get; set; }
    public CitadelTeleporterDestination Destination { get; set; }
}


public class CitadelTeleporterDestination
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public int Map { get; set; }
}
