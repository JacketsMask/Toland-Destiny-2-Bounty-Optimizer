using SQLiteEZMode.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace BountyVoiceControl.Resources
{
    // These are data structures for loading data from the sqlite database

    internal class BaseDbType
    {
        [SqliteCell(CellDataTypes.TEXT)]
        public string Type { get; set; }
        [SqliteCell(CellDataTypes.TEXT)]
        public string PhoneticOverrides { get; set; }

        public Choices GenerateChoices()
        {
            if (PhoneticOverrides != null)
            { // split the phonetic entries and add them to the choices
                return new Choices(PhoneticOverrides.Split(",", StringSplitOptions.TrimEntries));
            }
            else
            {
                return new Choices(Type);
            }
        }
    }

    [SqliteTable("AbilityTypes")]
    internal class AbilityTypes : BaseDbType { }

    [SqliteTable("ActivityTypes")]
    internal class ActivityTypes : BaseDbType { }

    [SqliteTable("AmmoTypes")]
    internal class AmmoTypes : BaseDbType { }

    [SqliteTable("DestinationTypes")]
    internal class DestinationTypes : BaseDbType { }

    [SqliteTable("ElementTypes")]
    internal class ElementTypes : BaseDbType { }

    [SqliteTable("EliminationTypes")]
    internal class EliminationTypes : BaseDbType { }

    [SqliteTable("EnemyModifierTypes")]
    internal class EnemyModifierTypes : BaseDbType { }

    [SqliteTable("EnemyTypes")]
    internal class EnemyTypes : BaseDbType { }

    [SqliteTable("PlayListTypes")]
    internal class PlayListTypes : BaseDbType { }

    [SqliteTable("WeaponTypes")]
    internal class WeaponTypes : BaseDbType { }
}
