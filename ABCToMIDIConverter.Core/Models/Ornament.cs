using System;

namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents different types of ornaments in ABC notation
    /// </summary>
    public enum OrnamentType
    {
        Trill,          // T
        Turn,           // S
        Mordent,        // M
        InvertedMordent, // ~M
        GraceNote,      // {note}
        Fermata,        // H
        Accent,         // >
        Staccato,       // .
        Tenuto,         // =
        Marcato         // ^
    }

    /// <summary>
    /// Base class for all ornaments that can be applied to notes
    /// </summary>
    public abstract class Ornament : MusicalElement
    {
        public OrnamentType Type { get; protected set; }
        public Note? TargetNote { get; set; } // The note this ornament applies to

        protected Ornament(OrnamentType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return $"{Type} ornament";
        }
    }

    /// <summary>
    /// Represents a trill ornament (rapid alternation between two adjacent notes)
    /// </summary>
    public class Trill : Ornament
    {
        public bool IsUpperTrill { get; set; } = true; // Default to upper note trill
        public int Interval { get; set; } = 1; // Semitone interval (default: whole tone)

        public Trill() : base(OrnamentType.Trill)
        {
        }

        public override string ToString()
        {
            return $"Trill ({(IsUpperTrill ? "upper" : "lower")}, interval: {Interval})";
        }
    }

    /// <summary>
    /// Represents a turn ornament (sequence of four notes around the main note)
    /// </summary>
    public class Turn : Ornament
    {
        public bool IsInverted { get; set; } = false; // Normal or inverted turn

        public Turn() : base(OrnamentType.Turn)
        {
        }

        public override string ToString()
        {
            return $"Turn ({(IsInverted ? "inverted" : "normal")})";
        }
    }

    /// <summary>
    /// Represents a mordent ornament (quick alternation with adjacent note)
    /// </summary>
    public class Mordent : Ornament
    {
        public bool IsInverted { get; set; } = false; // Upper or lower mordent
        public bool IsLong { get; set; } = false; // Short or long mordent

        public Mordent() : base(OrnamentType.Mordent)
        {
        }

        public override string ToString()
        {
            var type = IsInverted ? "inverted" : "upper";
            var length = IsLong ? "long" : "short";
            return $"Mordent ({type}, {length})";
        }
    }

    /// <summary>
    /// Represents grace notes (small notes played before the main note)
    /// </summary>
    public class GraceNotes : Ornament
    {
        public List<Note> Notes { get; set; } = new List<Note>();
        public bool IsAcciaccatura { get; set; } = false; // Crushed or grace note
        public double Duration { get; set; } = 0.125; // Default duration relative to main note

        public GraceNotes() : base(OrnamentType.GraceNote)
        {
        }

        public override string ToString()
        {
            var type = IsAcciaccatura ? "acciaccatura" : "appoggiatura";
            return $"Grace notes ({Notes.Count} notes, {type})";
        }
    }

    /// <summary>
    /// Represents articulation ornaments (accent, staccato, etc.)
    /// </summary>
    public class Articulation : Ornament
    {
        public double DurationMultiplier { get; set; } = 1.0; // How it affects note duration
        public double VelocityMultiplier { get; set; } = 1.0; // How it affects note velocity

        public Articulation(OrnamentType type) : base(type)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            switch (Type)
            {
                case OrnamentType.Staccato:
                    DurationMultiplier = 0.5; // Half duration
                    VelocityMultiplier = 1.1; // Slightly louder
                    break;
                case OrnamentType.Accent:
                    DurationMultiplier = 1.0;
                    VelocityMultiplier = 1.25; // 25% louder
                    break;
                case OrnamentType.Tenuto:
                    DurationMultiplier = 1.0; // Full duration
                    VelocityMultiplier = 1.0;
                    break;
                case OrnamentType.Marcato:
                    DurationMultiplier = 0.75; // Slightly detached
                    VelocityMultiplier = 1.4; // Much louder
                    break;
                case OrnamentType.Fermata:
                    DurationMultiplier = 2.0; // Hold longer
                    VelocityMultiplier = 1.0;
                    break;
            }
        }

        public override string ToString()
        {
            return $"{Type} (duration: {DurationMultiplier:F2}x, velocity: {VelocityMultiplier:F2}x)";
        }
    }
}
