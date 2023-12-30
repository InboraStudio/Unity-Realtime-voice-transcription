namespace LeastSquares.Undertone
{
    /// <summary>
    /// Represents a segment of speech with its corresponding text and start and end times.
    /// </summary>
    public class SpeechSegment
    {
        /// <summary>
        /// The text of the speech segment.
        /// </summary>
        public string text;

        /// <summary>
        /// The start time of the speech segment in milliseconds.
        /// </summary>
        public long t0;

        /// <summary>
        /// The end time of the speech segment in milliseconds.
        /// </summary>
        public long t1;

        /// <summary>
        /// The tokens emitted, each with its probability
        /// </summary>
        public SpeechToken[] tokens;
    }
}