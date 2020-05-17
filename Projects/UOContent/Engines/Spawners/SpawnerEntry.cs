using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.Spawners
{
  public class SpawnerEntry
  {
    public SpawnerEntry(string name, int probability, int maxcount)
    {
      SpawnedName = name;
      SpawnedProbability = probability;
      SpawnedMaxCount = maxcount;
      Spawned = new List<ISpawnable>();
    }

    public SpawnerEntry(BaseSpawner parent, IGenericReader reader)
    {
      int version = reader.ReadInt();

      SpawnedName = reader.ReadString();
      SpawnedProbability = reader.ReadInt();
      SpawnedMaxCount = reader.ReadInt();

      Properties = reader.ReadString();
      Parameters = reader.ReadString();

      int count = reader.ReadInt();

      Spawned = new List<ISpawnable>(count);

      for (int i = 0; i < count; ++i)
        // IEntity e = World.FindEntity( reader.ReadInt() );

        if (reader.ReadEntity() is ISpawnable e)
        {
          e.Spawner = parent;

          if (e is BaseCreature creature)
            creature.RemoveIfUntamed = true;

          Spawned.Add(e);

          if (!parent.Spawned.ContainsKey(e))
            parent.Spawned.Add(e, this);
        }
    }

    public int SpawnedProbability { get; set; }

    public int SpawnedMaxCount { get; set; }

    public string SpawnedName { get; set; }

    public string Properties { get; set; }

    public string Parameters { get; set; }

    public EntryFlags Valid { get; set; }

    public List<ISpawnable> Spawned { get; }

    public bool IsFull => Spawned.Count >= SpawnedMaxCount;

    public void Serialize(IGenericWriter writer)
    {
      writer.Write(0); // version

      writer.Write(SpawnedName);
      writer.Write(SpawnedProbability);
      writer.Write(SpawnedMaxCount);

      writer.Write(Properties);
      writer.Write(Parameters);

      writer.Write(Spawned.Count);

      for (int i = 0; i < Spawned.Count; ++i)
      {
        object o = Spawned[i];

        if (o is Item item)
          writer.Write(item);
        else if (o is Mobile mobile)
          writer.Write(mobile);
        else
          writer.Write(Serial.MinusOne);
      }
    }

    public void Defrag(BaseSpawner parent)
    {
      for (int i = 0; i < Spawned.Count; ++i)
      {
        ISpawnable spawned = Spawned[i];

        if (parent.OnDefragSpawn(spawned, false))
        {
          Spawned.RemoveAt(i--);
          parent.Spawned.Remove(spawned);
        }
      }
    }
  }
}
