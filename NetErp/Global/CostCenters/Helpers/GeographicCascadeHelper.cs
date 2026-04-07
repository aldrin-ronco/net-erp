using Models.Global;
using System.Collections.Generic;
using System.Linq;

namespace NetErp.Global.CostCenters.Shared
{
    /// <summary>
    /// Helpers puros para la cascada Country → Department → City.
    /// Reutilizado por CostCenterDetailViewModel y StorageDetailViewModel.
    /// </summary>
    public static class GeographicCascadeHelper
    {
        /// <summary>
        /// Selecciona el primer Department de un Country, o null si no hay.
        /// </summary>
        public static DepartmentGraphQLModel? FirstDepartment(CountryGraphQLModel? country)
        {
            return country?.Departments?.FirstOrDefault();
        }

        /// <summary>
        /// Selecciona el Department de un Country que coincida por Code, o el primero disponible.
        /// </summary>
        public static DepartmentGraphQLModel? DepartmentByCode(CountryGraphQLModel? country, string code)
        {
            if (country?.Departments is null) return null;
            return country.Departments.FirstOrDefault(d => d.Code == code) ?? country.Departments.FirstOrDefault();
        }

        /// <summary>
        /// Selecciona el primer City de un Department, o 0 si no hay.
        /// </summary>
        public static int FirstCityId(DepartmentGraphQLModel? department)
        {
            return department?.Cities?.FirstOrDefault()?.Id ?? 0;
        }

        /// <summary>
        /// Selecciona el City de un Department que coincida por Code, o el primero disponible.
        /// Retorna 0 si no hay cities.
        /// </summary>
        public static int CityIdByCode(DepartmentGraphQLModel? department, string code)
        {
            if (department?.Cities is null) return 0;
            return (department.Cities.FirstOrDefault(c => c.Code == code)
                ?? department.Cities.FirstOrDefault())?.Id ?? 0;
        }

        /// <summary>
        /// Dado un country Id, busca el Country en la lista y retorna la tupla (country, firstDept, firstCityId).
        /// Útil para inicializar defaults en SetForNew.
        /// </summary>
        public static (CountryGraphQLModel? Country, DepartmentGraphQLModel? Department, int CityId) FindDefaults(
            IEnumerable<CountryGraphQLModel>? countries,
            int defaultCountryId)
        {
            CountryGraphQLModel? country = countries?.FirstOrDefault(c => c.Id == defaultCountryId);
            DepartmentGraphQLModel? department = FirstDepartment(country);
            int cityId = FirstCityId(department);
            return (country, department, cityId);
        }
    }
}
