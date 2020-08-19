using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace com.cbctc.Service.Limfinity.Helpers
{
   public static class UdfsHelper
   {
      public static Dictionary<string, string> UdfsJsonElementToPropertyDictionary(JsonElement udfs)
      {
         return new Dictionary<string, string>
         (
            (from udf in udfs.EnumerateArray()
            from udfProperty in udf.EnumerateObject()
            let udfNamePropertyValue = udf.GetProperty("name").GetString()
            where udfProperty.Name != "name" && udfProperty.Name != "id"
            select udfProperty.Value.ValueKind switch
            {
               JsonValueKind.Object => from udfPropertyValueProperty in udfProperty.Value.EnumerateObject()
                                       select new KeyValuePair<string, string>
                                             (
                                                key: $"{udfNamePropertyValue}:{udfPropertyValueProperty.Name}",
                                                value: udfPropertyValueProperty.Value.ToString()
                                             ),
               _ => Enumerable.Repeat(new KeyValuePair<string, string>
                                       (
                                          key: $"{udfNamePropertyValue}",
                                          value: udfNamePropertyValue == "Index" ? $"{udfProperty.Value.GetSingle()}" : udfProperty.Value.ToString()
                                       ), 1),
            }).SelectMany(x => x)
         );
      }

      public static Dictionary<string, string> SubjectJsonElementToSubjectPropertyDictionary(JsonElement subject)
      {
         return new Dictionary<string, string>
         {
            { "id", subject.GetProperty("id").ToString() },
            { "name", subject.GetProperty("name").ToString() },
            { "barcode_tag", subject.GetProperty("barcode_tag").ToString() },
            { "rfid_tag", subject.GetProperty("rfid_tag").ToString() },
            { "subject_type:id", subject.GetProperty("subject_type").GetProperty("id").ToString() },
            { "subject_type:name", subject.GetProperty("subject_type").GetProperty("name").ToString() },
            { "created_at", subject.GetProperty("created_at").ToString() },
            { "updated_at", subject.GetProperty("updated_at").ToString() },
            { "terminated", subject.GetProperty("terminated").ToString() },
            { "created_by:id", subject.GetProperty("created_by").GetProperty("id").ToString() },
            { "created_by:username", subject.GetProperty("created_by").GetProperty("username").ToString() },
            { "updated_by:id", subject.GetProperty("updated_by").GetProperty("id").ToString() },
            { "updated_by:username", subject.GetProperty("updated_by").GetProperty("username").ToString() },
         };
      }
   }
}
