using System;

namespace com.cbctc.Service.Limfinity.Exceptions
{
   public class SampleMoveFailedException : Exception
   {
      public string TargetWell { get; set; }

      public SampleMoveFailedException(string targetWell)
      {
         TargetWell = targetWell;
      }
   }
}
