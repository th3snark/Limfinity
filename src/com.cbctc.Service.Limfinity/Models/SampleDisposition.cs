using System.Diagnostics;

namespace com.cbctc.Service.Limfinity.Models
{
   [DebuggerDisplay("{DebuggerDisplay,nq}")]
   public class SampleDisposition
   {
      public int Index { get; set; }
      public string Position { get; set; }
      public string Name { get; set; }
      public string SampleType { get; set; }
      public bool Terminated { get; set; }
      public bool Canceled { get; set; }
      public RootSample RootSample { get; set; }

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private string DebuggerDisplay
      {
         get { return $"{nameof(SampleDisposition)}: {Index} {Position} {Name} {SampleType}"; }
      }
   }
}
