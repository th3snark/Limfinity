using System;

namespace com.cbctc.Service.Limfinity.Exceptions
{
   public class SampleAlreadyExistsException : Exception
   {
      public SampleAlreadyExistsException(string sampleName,
         string plateSampleInformation, string results)
      {
         SampleName = sampleName;
         PlateSampleInformation = plateSampleInformation;
         Results = results;
      }

      public string Results { get; }
      public string SampleName { get; }
      public string PlateSampleInformation { get; }
   }
}
