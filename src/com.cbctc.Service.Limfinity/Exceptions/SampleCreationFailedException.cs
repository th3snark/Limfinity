using System;

namespace com.cbctc.Service.Limfinity.Exceptions
{
   public class SampleCreationFailedException : Exception
   {
      private readonly string _sampleName;

      public SampleCreationFailedException(string sampleName)
      {
         _sampleName = sampleName;
      }

      public string SampleName
      {
         get { return _sampleName; }
      }
   }
}
