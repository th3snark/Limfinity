using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace com.cbctc.Service.Limfinity.Models
{
   [DebuggerDisplay("{DebuggerDisplay,nq}")]
   public class User
   {
      public int Id { get; set; }
      public string UserName { get; set; }
      public string FullName { get; set; }
      public string Email { get; set; }
      public DateTime CreatedAt { get; set; }
      public IEnumerable<Role> Roles { get; set; }
      public bool Disabled { get; set; }
      public bool? Locked { get; set; }
      public bool Active { get; set; }
      public DateTime? LastLogin { get; set; }
      public bool Expired { get; set; }
      public DateTime? Expiration { get; set; }

      [DebuggerBrowsable(DebuggerBrowsableState.Never)]
      private string DebuggerDisplay
      {
         get { return $"{nameof(User)}: {Id} {UserName} {FullName}"; }
      }
   }
}
