using System;

namespace com.cbctc.Service.Limfinity.Exceptions
{
   public class InvalidSubjectTypeException : Exception
   {
      public InvalidSubjectTypeException(string subjectName, string expectedSubjectType, string actualSubjectType)
      {
         SubjectName = subjectName;
         ExpectedSubjectType = expectedSubjectType;
         ActualSubjectType = actualSubjectType;
      }

      public string SubjectName { get; }
      public string ExpectedSubjectType { get; }
      public string ActualSubjectType { get; }
   }
}
