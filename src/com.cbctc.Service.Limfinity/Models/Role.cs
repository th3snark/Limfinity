using System.Diagnostics;

namespace com.cbctc.Service.Limfinity.Models
{
   [DebuggerDisplay("{DebuggerDisplay,nq}")]
   public class Role
   {
      public int Id { get; set; }
      public string Name { get; set; }
      public bool AdminOption { get; set; }

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private string DebuggerDisplay
      {
         get { return $"{nameof(Role)}: {Id} {Name}"; }
      }
   }
}
