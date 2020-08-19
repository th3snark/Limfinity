using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using com.cbctc.Service.Limfinity.Models;
using Plate.Model;

namespace com.cbctc.Service.Limfinity
{
   public interface ILimfinityService
   {
      #region Authorization Token

      Task<string> GetAuthTokenAsync(CancellationToken token);

      #endregion

      #region Configuration

      Task<LimfinityConfiguration> GetLimfinityConfigurationAsync(CancellationToken token);

      #endregion

      #region Users

      Task<IEnumerable<User>> GetAllUsersAsync(int limit, CancellationToken token);
      Task<User> GetUserByIdAsync(int id, CancellationToken token);
      Task<User> GetUserByUserNameAsync(string username, CancellationToken token);

      #endregion

      #region Plate

      Task<IPlate> GetPlateByPlateNameAsync(string plateName, CancellationToken token);
      Task<string> GetRelatedPlatesBySampleNameAsync(string parentSampleName, CancellationToken token);
      Task<IPlate> CreatePlateAsync(string plateName, string plateType, IEnumerable<string> pctPositions, IEnumerable<string> ntcPositions, IEnumerable<string> necPositions, CancellationToken token);
      Task<bool> SetPlateTerminatedByPlateNameAsync(string plateName, bool terminated = true, CancellationToken token = default);
      Task<string> SetPcrSampleReferencesAsync(string plateName, bool updateFlag = false, CancellationToken token = default);

      #endregion

      #region Sample

      Task<string> GetSampleByUuidAsync(int uuid, CancellationToken token);
      Task<IEnumerable<Well>> GetSampleBySampleNameAsync(string sampleName, CancellationToken token);
      Task<IEnumerable<Well>> GetViralPrepSamplesByPlateNameAsync(string viralPlateName, int limit, CancellationToken token);
      Task<IEnumerable<(int Id, string Name)>> CreateViralPrepSampleAsync(string plateName, string swabSampleName, string wellPosition, int wellIndex, CancellationToken token);
      Task<string> MoveViralPlateSampleAsync(int viralPrepSampleUuid, string plateName, string swabSampleName, string newWellPosition, int newWellIndex, CancellationToken token);
      Task<IEnumerable<string>> GetVoidResultSwabSampleNames(IEnumerable<string> swabSampleNames, int limit, CancellationToken token);
      Task<IEnumerable<(int Id, string Name)>> CreateSwabSampleAsync(string swabSampleName, CancellationToken token);
      Task<bool> GetSampleExistsBySwabSampleNameAsync(string swabSampleName, CancellationToken token);
      Task<IEnumerable<SampleDisposition>> GetSampleDispositionsByPlateNameAsync(string plateName, CancellationToken token);
      Task<bool> SetSamplesTerminatedBySampleNamesAsync(IEnumerable<string> sampleNames, bool terminated, CancellationToken token);
      Task<bool> SetSwabSampleLastRnaPcrSampleAsync(IEnumerable<(string SwabSample, string LastRnaPcrSample)> swabSamplesInformationList, CancellationToken token);

      #endregion

      #region Test Results

      Task<string> SetTestResultToVoidForViralPrepSampleAsync(string viralPrepSampleName, string userComment, CancellationToken token);
      Task<string> SetTestResultToVoidForSwabSampleAsync(string swabSampleName, string userComment, CancellationToken token);
      Task<List<string>> GetTestResultsForSwabSampleAsync(string swabSampleName, int limit, CancellationToken token);
      Task<List<string>> GetTestResultsForSwabSamplesAsync(IEnumerable<string> swabSampleNames, int limit, CancellationToken token);

      #endregion

      #region Reagents

      Task<bool> UpdateReagentsAsync(int plateUUID, string proteinaseK, string nec, string lysisBuffer, string iec, string other, CancellationToken token);

      #endregion
   }
}
