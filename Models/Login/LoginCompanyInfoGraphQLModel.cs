using Models.Books;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Login
{
    public class LoginCompanyInfoGraphQLModel
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Status {  get; set; } = string.Empty;
        public string Address {  get; set; } = string.Empty;
        public string BusinessName {  get; set; } = string.Empty;
        public string CaptureType {  get; set; } = string.Empty;
        public CountryGraphQLModel Country { get; set; } = new();
        public DepartmentGraphQLModel Department { get; set; } = new();
        public CityGraphQLModel City { get; set; } = new();
        public string FirstLastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string FullName {  get; set; } = string.Empty;
        public string IdentificationNumber {  get; set; } = string.Empty;
        public IdentificationTypeGraphQLModel IdentificationType { get; set; } = new();
        public string MiddleLastName {  get; set; } = string.Empty;
        public string MiddleName {  get; set; } = string.Empty;
        public string PrimaryCellPhone {  get; set; } = string.Empty;
        public string SecondaryCellPhone { get; set; } = string.Empty;
        public string PrimaryPhone {  get; set; } = string.Empty;
        public string SecondaryPhone { get; set; } = string.Empty;
        public string Regime { get; set; } = string.Empty;
        public string SearchName { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string VerificationDigit { get; set; } = string.Empty;
        public string TelephonicInformation { get; set; } = string.Empty;
        public LoginLicenseGraphQLModel License { get; set; } = new();
        public DateTime UpdatedAt { get; set; } 
        public DateTime InsertedAt { get; set; }
    }
}
