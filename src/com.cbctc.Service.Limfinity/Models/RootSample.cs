using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace com.cbctc.Service.Limfinity.Models
{
   [DebuggerDisplay("{DebuggerDisplay,nq}")]
   public class RootSample
   {
      public string Name { get; set; }
      public string LastRnaPcrSample { get; set; }
      public TestResult TestResult { get; set; }

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private string DebuggerDisplay
      {
         get { return $"{nameof(RootSample)}: {Name}"; }
      }
   }
}
