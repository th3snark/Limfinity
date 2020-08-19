using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using com.cbctc.Service.Limfinity.Exceptions;
using com.cbctc.Service.Limfinity.Helpers;
using com.cbctc.Service.Limfinity.Models;
using Microsoft.Extensions.Options;
using Plate.Model;

//https://garywoodfine.com/making-api-calls-with-httpclientfactory-in-console-applications/

namespace com.cbctc.Service.Limfinity
{
   public class LimfinityService : ILimfinityService
   {
      private readonly HttpClient _httpClient;
      private readonly LimfinityServiceSettings _limfinityServiceSettings;

      #region Contruction

      public LimfinityService(HttpClient httpClient, IOptions<LimfinityServiceSettings> limfinityServiceSettings)
      {
         _httpClient = httpClient;
         _limfinityServiceSettings = limfinityServiceSettings.Value;
      }

      #endregion

      #region Authorization Token

      public async Task<string> GetAuthTokenAsync(CancellationToken token)
      {
         var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
         {
            { "username", _limfinityServiceSettings.Credentials.Username },
            { "password", _limfinityServiceSettings.Credentials.Password },
            { "method", "gen_token" },
         });

         var requestUrl = @"/api";

         Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

         var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
         request.Headers.Accept.Clear();
         request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
         request.Content = formUrlEncodedContent;

         var response = await _httpClient.SendAsync(request, token);

         response.EnsureSuccessStatusCode();

         var responseJsonString = await response.Content.ReadAsStringAsync();

         if (!ResponseHelper.SuccessfulResponse(responseJsonString))
         {
            string message = ResponseHelper.GetLimfinityMessage(responseJsonString);
            throw new LimfinityUnsucessfulException(message);
         }

         string authToken = null;
         using var jsonDocument = JsonDocument.Parse(responseJsonString);

         if (jsonDocument.RootElement.TryGetProperty("auth_token", out JsonElement authTokenElement))
         {
            authToken = jsonDocument.RootElement.GetProperty("auth_token").GetString();
         }
         return authToken;
      }

      private async Task<HttpResponseMessage> RetryOnInvalidToken(Func<Task<HttpResponseMessage>> operation, CancellationToken token)
      {
         var response = await operation();

         response.EnsureSuccessStatusCode();

         var responseJsonString = await response.Content.ReadAsStringAsync();
         using var jsonDocument = JsonDocument.Parse(responseJsonString);

         if (jsonDocument.RootElement.TryGetProperty("success", out JsonElement successElement) &&
             jsonDocument.RootElement.TryGetProperty("error", out JsonElement errorElement) &&
             jsonDocument.RootElement.TryGetProperty("message", out JsonElement messageElement))
         {
            var success = jsonDocument.RootElement.GetProperty("success").GetBoolean();
            var error = jsonDocument.RootElement.GetProperty("error").GetBoolean();
            var message = jsonDocument.RootElement.GetProperty("message").GetString();

            if (!success && error && (message == "Invalid token" || message == "Authentication Failed"))
            {
               _limfinityServiceSettings.Credentials.AuthToken = await GetAuthTokenAsync(token);
               response = await operation();
            }
         }

         return response;
      }

      #endregion

      #region Configuration

      public async Task<LimfinityConfiguration> GetLimfinityConfigurationAsync(CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "subjects" },
               { "subject_type", "Configuration" },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         using var jsonDocument = JsonDocument.Parse(jsonString);
         JsonElement udfs = jsonDocument.RootElement.GetProperty("Subjects")[0].GetProperty("udfs");

         var udfDictionary = UdfsHelper.UdfsJsonElementToPropertyDictionary(udfs);

         var pctPositionsJsonElement = JsonSerializer.Deserialize<JsonElement>(udfDictionary["PCT Positions"]);
         var ntcPositionsJsonElement = JsonSerializer.Deserialize<JsonElement>(udfDictionary["NTC Positions"]);
         var necPositionsJsonElement = JsonSerializer.Deserialize<JsonElement>(udfDictionary["NEC Positions"]);

         return new LimfinityConfiguration
         {
            PctPositions = pctPositionsJsonElement.EnumerateArray().Select(x => x.GetString()).ToArray(),
            NtcPositions = ntcPositionsJsonElement.EnumerateArray().Select(x => x.GetString()).ToArray(),
            NecPositions = necPositionsJsonElement.EnumerateArray().Select(x => x.GetString()).ToArray(),
         };
      }

      #endregion

      #region Users

      public async Task<IEnumerable<User>> GetAllUsersAsync(int limit, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "users" },
               { "limit", $"{limit}" },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         using JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
         var users = from u in jsonDocument.RootElement.GetProperty("Users").EnumerateArray()
                     select new User
                     {
                        Id = u.GetProperty("id").GetInt32(),
                        UserName = u.GetProperty("username").GetString(),
                        FullName = u.GetProperty("fullname").GetString(),
                        Email = u.GetProperty("email").GetString(),
                        CreatedAt = DateTime.Parse(u.GetProperty("created_at").GetString()),
                        Roles = (from r in u.GetProperty("roles").EnumerateArray()
                                 select new Role
                                 {
                                    Id = r.GetProperty("id").GetInt32(),
                                    Name = r.GetProperty("name").GetString(),
                                    AdminOption = r.GetProperty("admin_option").GetBoolean(),
                                 }).ToList(),
                        Disabled = u.GetProperty("disabled").GetBoolean(),
                        Locked = u.GetProperty("locked").ValueKind == JsonValueKind.Null ? (bool?)null : u.GetProperty("locked").GetBoolean(),
                        Active = u.GetProperty("active").GetBoolean(),
                        LastLogin = u.GetProperty("last_login").ValueKind == JsonValueKind.Null ? (DateTime?)null : DateTime.Parse(u.GetProperty("last_login").GetString()),
                        Expired = u.GetProperty("expired").GetBoolean(),
                        Expiration = u.GetProperty("expiration").ValueKind == JsonValueKind.Null ? (DateTime?)null : DateTime.Parse(u.GetProperty("expiration").GetString()),
                     };
         return users.ToList();
      }

      public async Task<User> GetUserByIdAsync(int id, CancellationToken token)
      {
         var users = await GetAllUsersAsync(Int32.MaxValue, token);
         return (from u in users
                 where u.Id == id
                 select u).FirstOrDefault();
      }

      public async Task<User> GetUserByUserNameAsync(string username, CancellationToken token)
      {
         var users = await GetAllUsersAsync(Int32.MaxValue, token);
         return (from u in users
                 where u.UserName == username
                 select u).FirstOrDefault();
      }

      #endregion

      #region Plate

      public async Task<IPlate> CreatePlateAsync(
         string plateName,
         string plateType,
         IEnumerable<string> pctPositions,
         IEnumerable<string> ntcPositions,
         IEnumerable<string> necPositions,
         CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var plateData = new Dictionary<string, string>
            {
               { "Name", plateName },
               { "BARCODE", plateName },
               { "Plate Type", plateType },
               { "PCT Positions Used", String.Join(",", pctPositions) },
               { "NTC Positions Used", String.Join(",", ntcPositions) },
               { "NEC Positions Used", String.Join(",", necPositions) },
            };

            var json = JsonSerializer.Serialize(plateData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Plate" },
               { "json", json },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SuccessfulResponse(jsonString))
         {
            await CreateControlSamples(
               plateName,
               pctPositions,
               ntcPositions,
               necPositions,
               token);

            return await GetPlateByPlateNameAsync(plateName, token);
         }

         return null;
      }

      public async Task<IPlate> GetPlateByPlateNameAsync(string plateName, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "subject_details" },
               { "barcode_tag", plateName },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SuccessfulResponse(jsonString))
         {
            return PlateModelHelper.GetPlate(jsonString);
         }

         return null;
      }

      public async Task<bool> SetPlateTerminatedByPlateNameAsync(string plateName, bool terminated = true, CancellationToken token = default)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var plateData = new Dictionary<string, object>
            {
               { "BARCODE", plateName },
               { "Terminated", terminated },
            };

            var json = JsonSerializer.Serialize(plateData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Plate" },
               { "json", json },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SuccessfulResponse(jsonString))
         {
            return true;
         }

         return false;
      }

      public async Task<string> SetPcrSampleReferencesAsync(string plateName, bool updateFlag = false, CancellationToken token = default)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var dataDictionary = new Dictionary<string, object>
            {
               { "plate_name", plateName },
               { "update_flag", updateFlag },
            };

            var dataJson = JsonSerializer.Serialize(dataDictionary);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "name", "ApiScript::SetPcrSampleReferences" },
               { "data",  dataJson},
            });

            var requestUrl = @"/api/run_script";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         return jsonString;
      }

      #endregion

      #region Samples

      /// <summary>
      /// Looks to see if a sample has already been registered on a plate
      /// </summary>
      /// <param name="swabSampleName">Sample name of the swab sample, eg ABC12345678</param>
      /// <param name="token"></param>
      /// <returns>
      ///   bool - if the sample pre-exists
      /// </returns>
      public async Task<bool> GetSampleExistsBySwabSampleNameAsync(string swabSampleName, CancellationToken token)
      {
         var existingSamples = await GetSampleBySampleNameAsync(swabSampleName, token);
         return existingSamples.Any();
      }

      public async Task<IEnumerable<Well>> GetSampleBySampleNameAsync(string sampleName, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "search_subjects" },
               { "subject_type", "Sample"},
               { "query", $"name = '{sampleName}'" },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         return PlateModelHelper.GetWells(jsonString);
      }

      private async Task<string> GetSamplesByParentSampleNameAsync(string sampleName, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "search_subjects" },
               { "subject_type", "Sample"},
               { "query", $"\"Parent Sample\" = '{sampleName}'" },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         return jsonString;
      }

      public async Task<string> GetSampleByUuidAsync(int uuid, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "search_subjects" },
               { "subject_type", "Sample"},
               { "query", $"id = {uuid}" },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         return jsonString;
      }

      /// <summary>
      /// Creates a swab sample and adds an associated viral plate sample 
      /// </summary>
      /// <param name="plateName"></param>
      /// <param name="swabSampleName"></param>
      /// <param name="wellPosition">eg A03</param>
      /// <param name="wellIndex">well index eg 4</param>
      /// <param name="token"></param>
      /// <returns>id/name of the 2 created samples</returns>
      public async Task<IEnumerable<(int Id, string Name)>> CreateViralPrepSampleAsync(
         string plateName,
         string swabSampleName,
         string wellPosition,
         int wellIndex,
         CancellationToken token)
      {
         //Ensure this is a new swab sample
         bool exists = await GetSampleExistsBySwabSampleNameAsync(swabSampleName, token);
         if (exists)
         {
            var results = await GetTestResultsForSwabSampleAsync(swabSampleName, 100, token);

            string resultString = string.Join(", ", results);
            string plateInformation = await GetRelatedPlatesBySampleNameAsync(swabSampleName, token);
            throw new SampleAlreadyExistsException(swabSampleName, plateInformation, resultString);
         }

         var response = await RetryOnInvalidToken(async () =>
         {
            var sampleData = new[]
            {
               new Dictionary<string, object>
               {
                  { "Name", swabSampleName },
                  { "BARCODE", swabSampleName },
                  { "Sample Type", "Swab" },
               },
               new Dictionary<string, object>
               {
                  { "Name", $"{plateName}_{wellPosition}" },
                  { "BARCODE", $"{plateName}_{wellPosition}"},
                  { "Sample Type", "Viral Prep" },
                  { "Parent Sample", swabSampleName },
                  { "Plate", plateName },
                  { "Index", wellIndex },
               },
            };

            var json = JsonSerializer.Serialize(sampleData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Sample"},
               { "json",  json}
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonSampleResponse = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SubjectsWereCreated(jsonSampleResponse))
         {
            return ResponseHelper.GetResponseIdsAndNames(jsonSampleResponse);
         }
         else
         {
            throw new SampleCreationFailedException(swabSampleName);
         }
      }

      /// <summary>
      /// Creates a swab sample  
      /// </summary>
      /// <param name="swabSampleName"></param>
      /// <param name="token"></param>
      /// <returns>json for the sample</returns>
      public async Task<IEnumerable<(int Id, string Name)>> CreateSwabSampleAsync(
         string swabSampleName,
         CancellationToken token)
      {


         var response = await RetryOnInvalidToken(async () =>
         {
            var sampleData = new[]
            {
               new Dictionary<string, object>
               {
                  { "Name", swabSampleName },
                  { "BARCODE", swabSampleName },
                  { "Sample Type", "Swab" },
               },
            };

            var json = JsonSerializer.Serialize(sampleData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Sample"},
               { "json",  json}
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonSampleResponse = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SubjectsWereCreated(jsonSampleResponse))
         {
            return ResponseHelper.GetResponseIdsAndNames(jsonSampleResponse);
         }
         else
         {
            throw new SampleCreationFailedException(swabSampleName);
         }
      }

      /// <summary>
      /// Move a viral pre
      /// </summary>
      /// <param name="viralPrepSampleUuid">the UUID of the viral prep sample</param>
      /// <param name="plateName"></param>
      /// <param name="newWellPosition">New location of the samlle eg A04</param>
      /// <param name="newWellIndex">new well index eg 5</param>
      /// <param name="token"></param>
      /// <returns>the sample JSON for the amended sample</returns>
      public async Task<string> MoveViralPlateSampleAsync(int viralPrepSampleUuid, string plateName, string swabSampleName, string newWellPosition, int newWellIndex, CancellationToken token)
      {
         //Create the replacement viral prep sample
         var response = await RetryOnInvalidToken(async () =>
         {
            var sampleData = new[]
            {
               new Dictionary<string, object>
               {
                  { "Name", $"{plateName}_{newWellPosition}" },
                  { "BARCODE", $"{plateName}_{newWellPosition}"},
                  { "Sample Type", "Viral Prep" },
                  { "Parent Sample", swabSampleName },
                  { "Plate", plateName },
                  { "Index", newWellIndex },
               },
            };

            var json = JsonSerializer.Serialize(sampleData);

            //Create new viral prep sample
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Sample"},
               { "json", json}
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         //Get the new sample details as a json to return to the caller
         var jsonSampleResponse = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SuccessfulResponse(jsonSampleResponse))
         {
            var newViralSampleID = ResponseHelper.GetResponseIdsAndNames(jsonSampleResponse).First().Id;

            var newSampleJson = await GetSampleByUuidAsync(newViralSampleID, token);

            //Delete the original viral prep sample
            var deleteSampleResponse = await RetryOnInvalidToken(async () =>
            {
               var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
               {
                  { "username", _limfinityServiceSettings.Credentials.Username },
                  { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
                  { "method", "delete_subject" },
                  { "id", viralPrepSampleUuid.ToString() },
               });

               var requestUrl = @"/api";

               //Uri requestUri = new Uri($"{_limfinityCredentials.BaseUrl}{requestUrl}");
               Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

               var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
               request.Headers.Accept.Clear();
               request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
               request.Content = formUrlEncodedContent;

               return await _httpClient.SendAsync(request, token);

            }, token);

            deleteSampleResponse.EnsureSuccessStatusCode();

            return newSampleJson;
         }
         else
         {
            throw new SampleMoveFailedException(newWellPosition);
         }
      }

      public async Task<IEnumerable<Well>> GetViralPrepSamplesByPlateNameAsync(string viralPlateName, int limit, CancellationToken token)
      {
         var viralPlateSamplesResponse = await RetryOnInvalidToken(async () =>
         {
            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "search_subjects" },
               { "subject_type", "Sample"},
               { "query", $"Plate = '{viralPlateName}'" },
               { "limit", $"{limit}" },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         viralPlateSamplesResponse.EnsureSuccessStatusCode();

         var viralPlateSamplesJson = await viralPlateSamplesResponse.Content.ReadAsStringAsync();

         return PlateModelHelper.GetWells(viralPlateSamplesJson);
      }

      private async Task<string> CreateControlSamples(
         string plateName,
         IEnumerable<string> pctPositions,
         IEnumerable<string> ntcPositions,
         IEnumerable<string> necPositions,
         CancellationToken token)
      {
         //TODO: Iterate each of the control IEnumerables. This code assumes just one entry in each
         var pctPosition = pctPositions.ElementAt(0);
         var ntcPosition = ntcPositions.ElementAt(0);
         var necPosition = necPositions.ElementAt(0);

         var pctIndex = Plate96.GetIndexByPosition(pctPosition);
         var ntcIndex = Plate96.GetIndexByPosition(ntcPosition);
         var necIndex = Plate96.GetIndexByPosition(necPosition);

         var response = await RetryOnInvalidToken(async () =>
         {
            var controlSampleData = new[]
            {
               new Dictionary<string, object>
               {
                  { "Name", $"{plateName}_{ntcPosition}" },
                  { "BARCODE", $"{plateName}_{ntcPosition}"},
                  { "Sample Type", "NTC" },
                  { "Parent Sample", "NTC" },
                  { "Plate", plateName },
                  { "Index", ntcIndex },
               },
               new Dictionary<string, object>
               {
                  { "Name", $"{plateName}_{necPosition}" },
                  { "BARCODE", $"{plateName}_{necPosition}"},
                  { "Sample Type", "NEC" },
                  { "Parent Sample", "NEC" },
                  { "Plate", plateName },
                  { "Index", necIndex },
               },
            };

            var json = JsonSerializer.Serialize(controlSampleData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Sample"},
               { "json", json },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         return jsonString;
      }

      public async Task<IEnumerable<string>> GetVoidResultSwabSampleNames(IEnumerable<string> swabSampleNames, int limit, CancellationToken token)
      {
         string resultResponseJson = await GetTestResultsForSwabSamplesJsonString(swabSampleNames, limit, token);

         using JsonDocument resultsJsonDocument = JsonDocument.Parse(resultResponseJson);

         var swabsWithVoidResult = from r in resultsJsonDocument.RootElement.GetProperty("Subjects").EnumerateArray()
                                   let u = UdfsHelper.UdfsJsonElementToPropertyDictionary(r.GetProperty("udfs"))
                                   where u["Result"] == "Void"
                                   select u["Sample:name"];

         return swabsWithVoidResult.ToList();
      }

      private async Task<string> GetTestResultsForSwabSamplesJsonString(IEnumerable<string> swabSampleNames, int limit, CancellationToken token)
      {
         var testResultsResponse = await RetryOnInvalidToken(async () =>
         {
            var swabSampleNamesString = $"'{String.Join("','", swabSampleNames)}'";
            if (swabSampleNames.Count() > 1)
            {
               swabSampleNamesString = $"({swabSampleNamesString})";
            }

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "search_subjects" },
               { "subject_type", "Test Result"},
               { "query", $"Sample = {swabSampleNamesString}" },
               { "limit", $"{limit}" },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         testResultsResponse.EnsureSuccessStatusCode();

         var resultResponseJson = await testResultsResponse.Content.ReadAsStringAsync();
         return resultResponseJson;
      }

      public async Task<IEnumerable<SampleDisposition>> GetSampleDispositionsByPlateNameAsync(string plateName, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var dataDictionary = new Dictionary<string, object>
            {
               { "plate_name", plateName },
            };

            var dataJson = JsonSerializer.Serialize(dataDictionary);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "name", "ApiScript::GetSampleDispositionsWithResults" },
               { "data",  dataJson},
            });

            var requestUrl = @"/api/run_script";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         using JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
         var sampleDispositions = from s in jsonDocument.RootElement.GetProperty("ret").GetProperty("result").GetProperty("samples").EnumerateArray()
                                  let index = s.GetProperty("index").GetInt32()
                                  let rs = s.GetProperty("root_sample")
                                  orderby index
                                  select new SampleDisposition
                                  {
                                     Index = index,
                                     Position = s.GetProperty("position").GetString(),
                                     Name = s.GetProperty("name").GetString(),
                                     SampleType = s.GetProperty("sample_type").GetString(),
                                     Terminated = s.GetProperty("terminated").GetBoolean(),
                                     Canceled = s.GetProperty("canceled").ValueKind != JsonValueKind.Null && s.GetProperty("canceled").GetBoolean(),
                                     RootSample = new RootSample
                                     {
                                        Name = rs.GetProperty("name").GetString(),
                                        LastRnaPcrSample = rs.TryGetProperty("last_rna_pcr_sample", out var lastRnaPcrSampleJsonElement) ? lastRnaPcrSampleJsonElement.GetString() : null,
                                        TestResult = rs.TryGetProperty("test_result", out var testResultJsonElement) ? new TestResult
                                        {
                                           Name = testResultJsonElement.ValueKind != JsonValueKind.Null ? testResultJsonElement.GetProperty("name").GetString() : null,
                                           Result = testResultJsonElement.ValueKind != JsonValueKind.Null ? testResultJsonElement.GetProperty("result").GetString() : null,
                                           ResultInfo = testResultJsonElement.ValueKind != JsonValueKind.Null ? testResultJsonElement.GetProperty("result_info").GetString() : null,
                                           DateTested = testResultJsonElement.ValueKind != JsonValueKind.Null ? testResultJsonElement.GetProperty("date_tested").GetString() : null,
                                           RnaPcrSample = testResultJsonElement.ValueKind != JsonValueKind.Null ? testResultJsonElement.GetProperty("rna_pcr_sample").GetString() : null,
                                        } : null,
                                     },
                                  };
         return sampleDispositions.ToList();
      }

      public async Task<bool> SetSamplesTerminatedBySampleNamesAsync(IEnumerable<string> sampleNames, bool terminated = true, CancellationToken token = default)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var sampleData = from sampleName in sampleNames
                             select new Dictionary<string, object>
                             {
                                { "BARCODE", sampleName },
                                { "Terminated", terminated },
                             };

            var json = JsonSerializer.Serialize(sampleData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Sample" },
               { "json", json },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SuccessfulResponse(jsonString))
         {
            return true;
         }

         return false;
      }

      public async Task<bool> SetSwabSampleLastRnaPcrSampleAsync(IEnumerable<(string SwabSample, string LastRnaPcrSample)> swabSampleInformationList, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var sampleData = from swabSampleInformation in swabSampleInformationList
                             select new Dictionary<string, object>
                             {
                                { "BARCODE", swabSampleInformation.SwabSample },
                                { "Last RNA-PCR Sample", swabSampleInformation.LastRnaPcrSample },
                             };

            var json = JsonSerializer.Serialize(sampleData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Sample" },
               { "json", json },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonString = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SuccessfulResponse(jsonString))
         {
            return true;
         }

         return false;
      }

      #endregion

      #region Test Result

      public async Task<List<string>> GetTestResultsForSwabSampleAsync(string swabSampleName, int limit, CancellationToken token)
      {
         return await GetTestResultsForSwabSamplesAsync(new List<string> { swabSampleName }, limit, token);
      }

      public async Task<List<string>> GetTestResultsForSwabSamplesAsync(IEnumerable<string> swabSampleNames, int limit, CancellationToken token)
      {
         var resultResponseJson = await GetTestResultsForSwabSamplesJsonString(swabSampleNames, limit, token);

         using JsonDocument resultsJsonDocument = JsonDocument.Parse(resultResponseJson);
         var results = (from r in resultsJsonDocument.RootElement.GetProperty("Subjects").EnumerateArray()
                        let u = UdfsHelper.UdfsJsonElementToPropertyDictionary(r.GetProperty("udfs"))
                        select u["Result"]).ToList();

         return results;
      }

      public async Task<string> SetTestResultToVoidForViralPrepSampleAsync(string viralPrepSampleName, string userComment, CancellationToken token)
      {
         var viralPrepSample = await GetSampleBySampleNameAsync(viralPrepSampleName, token);

         //Get swab sample name for the viral prep sample
         var swabSampleNames = from s in viralPrepSample
                               select s.Properties["Parent Sample:name"];

         string swabSampleName = swabSampleNames.First();

         //Check there is no result for the sample
         var resultsForSample = await GetTestResultsForSwabSampleAsync(swabSampleName, 100, token);

         int jsonResultCount = resultsForSample.Count();

         //Already has a result, quit
         if (jsonResultCount > 0)
         {
            throw new VoidingFailedException(swabSampleName);
         }

         //Void the swab sample
         var response = await SetTestResultToVoidForSwabSampleAsync(swabSampleName, userComment, token);

         return response;
      }

      public async Task<string> SetTestResultToVoidForSwabSampleAsync(string swabSampleName, string userComment, CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var swabSampleData = new[]
            {
               new Dictionary<string, object>
               {
                  { "Sample", swabSampleName },
                  { "Result", "Void"},
                  { "Result Info", userComment },
                  { "Date Tested", $"{DateTime.Now:dd/MM/yyyy, HH:mm:ss}" },
               }
            };

            var json = JsonSerializer.Serialize(swabSampleData);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Test Result"},
               { "json", json },
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);

         }, token);

         response.EnsureSuccessStatusCode();

         var jsonResult = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SubjectsWereCreated(jsonResult))
         {
            return jsonResult;
         }
         else
         {
            throw new VoidingFailedException(swabSampleName);
         }
      }

      /// <summary>
      /// returns a list of Plate / Sample ID / Sample Type for all samples that are
      /// descendants of the parentSample.
      /// searching for all samples by root sample is very slow in limfinity
      /// so this recursive method finds samples via the sample:parent sample relationship, which is quick
      /// </summary>
      /// <param name="parentSampleName"></param>
      /// <param name="token"></param>
      /// <returns></returns>
      public async Task<string> GetRelatedPlatesBySampleNameAsync(string parentSampleName, CancellationToken token)
      {
         string plateNames = string.Empty;

         var childSamples = await GetSamplesByParentSampleNameAsync(parentSampleName, token);

         using JsonDocument childSamplesDocument = JsonDocument.Parse(childSamples);

         foreach (var childSample in childSamplesDocument.RootElement.GetProperty("Subjects").EnumerateArray())
         {
            var udfs = UdfsHelper.UdfsJsonElementToPropertyDictionary(childSample.GetProperty("udfs"));
            string sampleName = childSample.GetProperty("name").GetString();
            plateNames += $"Plate: {udfs["Plate:name"]}, Sample: {sampleName} ({udfs["Sample Type"]}){Environment.NewLine}";

            plateNames += await GetRelatedPlatesBySampleNameAsync(sampleName, token);
         }

         return plateNames;
      }

      #endregion

      #region Reagents

      public async Task<bool> UpdateReagentsAsync(
         int plateUUID,
         string ProteinaseK,
         string Nec,
         string LysisBuffer,
         string Iec,
         string other,
         CancellationToken token)
      {
         var response = await RetryOnInvalidToken(async () =>
         {
            var reagentUpdateProperties = new Dictionary<string, object>
            {
               { "UID", plateUUID },
               { "Proteinase k Reagent", ProteinaseK},
               { "Nuclease-Free Water (NEC)", Nec},
               { "Lysis-Buffer", LysisBuffer},
               { "Internal Extraction-Control", Iec},
               { "other", other},
            };

            var json = JsonSerializer.Serialize(reagentUpdateProperties);

            var formUrlEncodedContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
               { "username", _limfinityServiceSettings.Credentials.Username },
               { "auth_token", _limfinityServiceSettings.Credentials.AuthToken },
               { "method", "import_subjects" },
               { "subject_type", "Plate"},
               { "json", json }
            });

            var requestUrl = @"/api";

            Uri requestUri = new Uri($"{_limfinityServiceSettings.BaseUrl}{requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formUrlEncodedContent;

            return await _httpClient.SendAsync(request, token);
         }, token);

         response.EnsureSuccessStatusCode();

         var jsonReagentsString = await response.Content.ReadAsStringAsync();

         if (ResponseHelper.SuccessfulResponse(jsonReagentsString))
         {
            return true;
         }
         else
         {
            string errorMessage = ResponseHelper.GetLimfinityMessage(jsonReagentsString);
            throw new LimfinityUnsucessfulException(errorMessage);
         }
      }

      #endregion
   }
}
