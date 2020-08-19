using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace com.cbctc.Service.Limfinity.Helpers
{
   internal static class ResponseHelper
   {
      public static IEnumerable<(int Id, string Name)> GetResponseIdsAndNames(string jsonSampleResponse)
      {
         JsonDocument jsonDocument = JsonDocument.Parse(jsonSampleResponse);
         var ids = jsonDocument.RootElement.GetProperty("ids").GetString().Split(",");
         var names = jsonDocument.RootElement.GetProperty("names").GetString().Split(",");
         var tuple = ids.Select((id, index) => (Convert.ToInt32(id), names[index]));
         return tuple;
      }

      public static bool SubjectsWereCreated(string jsonSampleResponse)
      {
         JsonDocument jsonDocument = JsonDocument.Parse(jsonSampleResponse);
         if (jsonDocument.RootElement.TryGetProperty("ids", out JsonElement idsJsonElement))
         {
            return idsJsonElement.GetString() != string.Empty;
         }
         return false;
      }

      //public static List<string> GetSwabSampleNamesForViralPrepSamples(string samplesJsonString)
      //{
      //   //Get results for the swabs associated with the sampled
      //   using JsonDocument jsonDocument = JsonDocument.Parse(samplesJsonString);

      //   JsonElement viralPlateSamples = jsonDocument.RootElement.GetProperty("Subjects");

      //   var swabSamples = from s in viralPlateSamples.EnumerateArray()
      //                     from u in s.GetProperty("udfs").EnumerateArray()
      //                     where (u.GetProperty("name").GetString() == "Parent Sample")
      //                     select u.GetProperty("value").GetProperty("name").GetString();

      //   return swabSamples.ToList();
      //}

      public static bool SuccessfulResponse(string responseJsonString)
      {
         using var jsonDocument = JsonDocument.Parse(responseJsonString);

         if (jsonDocument.RootElement.TryGetProperty("success", out JsonElement successElement))
         {
            var success = jsonDocument.RootElement.GetProperty("success").GetBoolean();
            return success;
         }

         //Return true if the json doesnt contain success property
         return true;
      }

      public static string GetLimfinityMessage(string responseJsonString)
      {
         using var jsonDocument = JsonDocument.Parse(responseJsonString);

         // error messages can be found in property 'msg' or 'message'

         if (jsonDocument.RootElement.TryGetProperty("msg", out JsonElement msgElement))
         {
            var message = msgElement.GetString();
            return message;
         }

         if (jsonDocument.RootElement.TryGetProperty("message", out JsonElement messageElement))
         {
            var message = messageElement.GetString();
            return message;
         }

         return string.Empty;
      }
   }
}
