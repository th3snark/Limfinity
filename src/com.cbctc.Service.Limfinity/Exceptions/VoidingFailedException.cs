using System;

namespace com.cbctc.Service.Limfinity.Exceptions
{
   public class VoidingFailedException : Exception
   {
      public string SwabSampleName { get; }

      public VoidingFailedException(string swabSampleName)
      {
         SwabSampleName = swabSampleName;
      }
   }
}
