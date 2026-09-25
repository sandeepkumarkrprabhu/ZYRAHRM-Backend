using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Zyra.LantimeServiceApp.Models;

namespace ZyraHangfireService
{
    public static class EmployeeCustomCodeMapper
    {
        public static EmployeeMapperDto GetPumexEmployeeCode(string lantimeCode)
        {
            var mapperFilePath = Path.Combine(AppContext.BaseDirectory, "Mapper", "EmployeeCodeMapper.csv");
            var records = ReadCsv(mapperFilePath);
            var selectedEmployeeCode = records.Where(f => f.UserId == Convert.ToInt32(lantimeCode)).FirstOrDefault();
            return selectedEmployeeCode;
        }

        public static List<EmployeeMapperDto> ReadCsv(string filePath)
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            var records = csv.GetRecords<EmployeeMapperDto>().ToList();
            return records;
        }
    }
}
