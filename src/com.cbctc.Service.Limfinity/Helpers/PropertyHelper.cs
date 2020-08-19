using System.Collections.Generic;

namespace com.cbctc.Service.Limfinity.Helpers
{
   public static class PropertyHelper
   {
      public static Dictionary<string, (string Name, bool Visible)> PlatePropertyDictionary = new Dictionary<string, (string, bool)>
      {
         { "id", ("Plate Id", true) },
         { "name", ("Plate Name", true) },
         { "barcode_tag", ("Barcode Tag", false) },
         { "rfid_tag", ("RFID Tag", false) },
         { "subject_type:id", ("Subject Type Id", false) },
         { "subject_type:name", ("Subject Type", false) },
         { "created_at", ("Created", true) },
         { "updated_at", ("Updated", true) },
         { "terminated", ("Archived", true) },
         { "created_by:id", ("Created By Id", false) },
         { "created_by:username", ("Created By", true) },
         { "updated_by:id", ("Updated By Id", false) },
         { "updated_by:username", ("Updated By", true) },
         { "Canceled", ("Canceled", true) },

         { "Plate Type", ("Plate Type", true) },
         { "Samples", ("Sample Count", true) },
         { "NTC Positions Used", ("NTC Positions Used", true) },
         { "NEC Positions Used", ("NEC Positions Used", true) },
         { "PCT Positions Used", ("PCT Positions Used", true) },
      };

      public static Dictionary<string, (string Name, bool Visible)> SamplePropertyDictionary = new Dictionary<string, (string, bool)>
      {
         { "id", ("Sample Id", true) },
         { "name", ("Sample Name", true) },
         { "barcode_tag", ("Barcode Tag", false) },
         { "rfid_tag", ("RFID Tag", false) },
         { "subject_type:id", ("Subject Type Id", false) },
         { "subject_type:name", ("Subject Type", false) },
         { "created_at", ("Created", true) },
         { "updated_at", ("Updated", true) },
         { "terminated", ("Archived", true) },
         { "created_by:id", ("Created By Id", false) },
         { "created_by:username", ("Created By", true) },
         { "updated_by:id", ("Updated By Id", false) },
         { "updated_by:username", ("Updated By", true) },
         { "Canceled", ("Canceled", true) },
         { "Parent Sample:id", ("Root Sample Id", true) },
         { "Parent Sample:name", ("Root Sample Name", true) },
         { "Root Sample:id", ("Root Sample Id", true) },
         { "Root Sample:name", ("Root Sample Name", true) },
         { "Root Sample:Last RNA-PCR Sample", ("Root Sample Last RNA-PCR Sample", true) },
         { "Condition", ("Condition Count", false) },
         { "Sample Type", ("Sample Type", true) },
         { "Plate:id", ("Plate Id", true) },
         { "Plate:name", ("Plate Name", true) },
         { "Index", ("Index", true) },
         { "Test Results", ("Test Result Count", false) },
         { "Parent Sample:Test Result:name", ("Test Result Name", true) },
         { "Parent Sample:Test Result:Result", ("Test Result", true) },
         { "Parent Sample:Test Result:Result Info", ("Test Result Info", true) },
         { "Parent Sample:Test Result:Date Tested", ("Test Result Date Tested", true) },
         { "Parent Sample:Test Result:RNA-PCR Sample", ("Test Result RNA-PCR Sample", true) },
         { "Position", ("Position", true) },
      };
   }
}
