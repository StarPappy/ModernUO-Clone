/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ISerializable.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server
{
    public interface ISerializable
    {
        BufferWriter SaveBuffer { get; set; }
        int TypeRef { get; }
        Serial Serial { get; }
        void Serialize();
        void Deserialize(IGenericReader reader);
        void Serialize(IGenericWriter writer);
        void Delete();
        bool Deleted { get; }
    }
}
