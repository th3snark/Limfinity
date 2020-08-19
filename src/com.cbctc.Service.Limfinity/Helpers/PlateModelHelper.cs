using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.cbctc.Service.Limfinity.Exceptions;
using Plate.Model;

namespace com.cbctc.Service.Limfinity.Helpers
{
   public static class PlateModelHelper
   {
      public static IPlate GetPlate(string plateJsonString)
      {
         var jsonDocument = JsonDocument.Parse(plateJsonString);
         var rootElement = jsonDocument.RootElement;

         var name = rootElement.GetProperty("name").GetString();
         var properties = (
                             UdfsHelper.SubjectJsonElementToSubjectPropertyDictionary(rootElement)
                          )
                          .Concat
                          (
                             UdfsHelper.UdfsJsonElementToPropertyDictionary(jsonDocument.RootElement.GetProperty("udfs"))
                          )
                          .ToDictionary(x => x.Key, x => x.Value);

         if (!properties.ContainsKey("Plate Type"))
         {
            throw new InvalidSubjectTypeException(name, "Plate", properties["subject_type:name"]);
         }

         var plateType = properties["Plate Type"];
         IPlate plate = plateType switch
         {
            "Viral Prep Plate" => new Plate96(name, properties),
            "RNA Plate" => new Plate96(name, properties),
            "PCR Plate" => new Plate384(name, properties),
            _ => throw new Exception($"Unhandled {nameof(plateType)}: {plateType}"),
         };

         return plate;
      }

      public static IEnumerable<Well> GetWells(string samplesJsonString)
      {
         var jsonDocument = JsonDocument.Parse(samplesJsonString);
         var wells = (from subject in jsonDocument.RootElement.GetProperty("Subjects").EnumerateArray()
                      select new Well(
                        name: subject.GetProperty("name").ToString(),
                        properties: (
                                       UdfsHelper.SubjectJsonElementToSubjectPropertyDictionary(subject)
                                    )
                                    .Concat
                                    (
                                       UdfsHelper.UdfsJsonElementToPropertyDictionary(subject.GetProperty("udfs"))
                                    ).ToDictionary(x => x.Key, x => x.Value)
                      )).OrderBy(w => WellIndex(w));
         return wells;
      }

      private static int WellIndex(Well w)
      {
         if (w.Properties.ContainsKey("Index"))
         {
            return Convert.ToInt32(w.GetProperty("Index"));
         }
         else
         {
            return 0;
         }
      }
   }
}
