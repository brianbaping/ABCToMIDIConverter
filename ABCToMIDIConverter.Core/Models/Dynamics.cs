namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents dynamics (volume markings) in ABC notation
    /// </summary>
    public class Dynamics : MusicalElement
    {
        /// <summary>
        /// The type of dynamic marking
        /// </summary>
        public DynamicType Type { get; set; }

        /// <summary>
        /// The text representation of the dynamic (e.g., "pp", "f", "crescendo")
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// MIDI velocity value (0-127) that this dynamic represents
        /// </summary>
        public int Velocity => GetVelocityForDynamic(Type);

        /// <summary>
        /// How long this dynamic marking affects subsequent notes (in measures)
        /// 0 means it affects all following notes until another dynamic is encountered
        /// </summary>
        public int Duration { get; set; } = 0;

        public Dynamics(DynamicType type, string text = "")
        {
            Type = type;
            Text = string.IsNullOrEmpty(text) ? GetDefaultTextForDynamic(type) : text;
        }

        /// <summary>
        /// Gets the MIDI velocity value for a dynamic type
        /// </summary>
        private static int GetVelocityForDynamic(DynamicType type)
        {
            return type switch
            {
                DynamicType.Pianississimo => 16,    // ppp - very, very soft
                DynamicType.Pianissimo => 32,       // pp - very soft
                DynamicType.Piano => 48,            // p - soft
                DynamicType.MezzoPiano => 64,       // mp - moderately soft
                DynamicType.MezzoForte => 80,       // mf - moderately loud (default)
                DynamicType.Forte => 96,            // f - loud
                DynamicType.Fortissimo => 112,      // ff - very loud
                DynamicType.Fortississimo => 127,   // fff - very, very loud
                DynamicType.Crescendo => 80,        // Start at moderate, will increase
                DynamicType.Diminuendo => 80,       // Start at moderate, will decrease
                DynamicType.Sforzando => 110,       // Sudden accent
                DynamicType.Accent => 100,          // Emphasized note
                _ => 80 // Default mf
            };
        }

        /// <summary>
        /// Gets the default text representation for a dynamic type
        /// </summary>
        private static string GetDefaultTextForDynamic(DynamicType type)
        {
            return type switch
            {
                DynamicType.Pianississimo => "ppp",
                DynamicType.Pianissimo => "pp",
                DynamicType.Piano => "p",
                DynamicType.MezzoPiano => "mp",
                DynamicType.MezzoForte => "mf",
                DynamicType.Forte => "f",
                DynamicType.Fortissimo => "ff",
                DynamicType.Fortississimo => "fff",
                DynamicType.Crescendo => "crescendo",
                DynamicType.Diminuendo => "diminuendo",
                DynamicType.Sforzando => "sfz",
                DynamicType.Accent => ">",
                _ => "mf"
            };
        }

        public override string ToString()
        {
            return $"{Type} ({Text}) - Velocity: {Velocity}";
        }
    }

    /// <summary>
    /// Types of dynamic markings in music
    /// </summary>
    public enum DynamicType
    {
        // Standard dynamics (volume levels)
        Pianississimo,  // ppp - very, very soft
        Pianissimo,     // pp - very soft  
        Piano,          // p - soft
        MezzoPiano,     // mp - moderately soft
        MezzoForte,     // mf - moderately loud (default)
        Forte,          // f - loud
        Fortissimo,     // ff - very loud
        Fortississimo,  // fff - very, very loud

        // Gradual changes
        Crescendo,      // Getting louder
        Diminuendo,     // Getting softer (also decrescendo)

        // Accents and special effects
        Sforzando,      // sfz - sudden accent
        Accent          // > - emphasized note
    }
}
