using System;
using System.Diagnostics;

namespace com.cbctc.Service.Limfinity.Models
{
   [DebuggerDisplay("{DebuggerDisplay,nq}")]
   public class TestResult
   {
      public string Name { get; set; }
      public string Result { get; set; }
      public string ResultInfo { get; set; }
      public string DateTested { get; set; }
      public string RnaPcrSample { get; set; }

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private string DebuggerDisplay
      {
         get { return $"{nameof(RootSample)}: {Name}, {Result}"; }
      }
   }
}
