using System;

namespace com.cbctc.Service.Limfinity.Exceptions
{
   public class LimfinityUnsucessfulException : Exception
   {
      public LimfinityUnsucessfulException(string limfinityErrorMessage)
      {
         LimfinityErrorMessage = limfinityErrorMessage;
      }

      public string LimfinityErrorMessage { get; }
   }
}
