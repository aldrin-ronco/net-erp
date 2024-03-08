using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common.Extensions
{
    public static class Extensions
    {
        public static string Between(this string STR, string FirstString, string LastString)
        {
            if (!STR.Contains(FirstString) && !STR.Contains(LastString)) { return STR; }
            string FinalString = "";
            int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
            int Pos2 = STR.IndexOf(LastString);
            FinalString = STR.Substring(Pos1, Pos2 - Pos1);
            return FinalString;
        }

        /// <summary>
        /// Allows to clone a list without reference
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Clone<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (source == null)
            {
                return default;
            }

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        }

        /// <sumary>
        /// Valida si la cadena corresponde a un correo electronico valido
        /// </sumary>
        public static bool IsValidEmail(this string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Remueve espacios extras en una cadema, por ejemplo
        /// La cadena : "CUENTA  CONTABLE   NUEVA", Retornaria el valor "CUENTA CONTABLE NUEVA"
        /// </summary>
        /// <param name="str">Cadena inyectada por extension de string</param>
        /// <returns></returns>
        public static string RemoveExtraSpaces(this string str)
        {
            return _RemoveExtraSpaces(str);
        }

        private static string _RemoveExtraSpaces(string str)
        {
            if (str.Contains("  "))
            {
                return RemoveExtraSpaces(str.Replace("  ", " "));
            }
            else
            {
                return str;
            }
        }

        /// <summary>
        /// Implementa busqueda en IEnumerable al estilo SQL Server LIKE%% OPERATOR
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool IsSqlLikeMatch(this string input, string pattern)
        {
            /* Turn "off" all regular expression related syntax in
            * the pattern string. */
            pattern = Regex.Escape(pattern);

            /* Replace the SQL LIKE wildcard metacharacters with the
            * equivalent regular expression metacharacters. */
            pattern = pattern.Replace("%", ".*?").Replace("_", ".");

            /* The previous call to Regex.Escape actually turned off
            * too many metacharacters, i.e. those which are recognized by
            * both the regular expression engine and the SQL LIKE
            * statement ([...] and [^...]). Those metacharacters have
            * to be manually unescaped here. */
            pattern = pattern.Replace(@"\[", "[").Replace(@"\]",
            "]").Replace(@"\^", "^");

            return Regex.IsMatch(input, pattern);
        }

        /// <summary>
        /// Formatea strings de numeros telefonicos para mejorar legibilidad
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="format"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToPhoneFormat(this string phoneNumber, string format = "### ### ## ##", string separator = "-")
        {
            int posChar = 0, posFmt = 0;
            string formatedString = "", initialPhoneNumber = phoneNumber;

            if (string.IsNullOrEmpty(phoneNumber)) return string.Empty;

            // Remover espacios a la cadena
            phoneNumber = phoneNumber.Replace(" ", "").Replace(Convert.ToChar(9).ToString(), "");
            // Remover , = 44 y ; = 59
            phoneNumber = phoneNumber.Replace(Convert.ToChar(44).ToString(), "").Replace(Convert.ToChar(59).ToString(), "");
            // Remover - = 45 y _ = 95
            phoneNumber = phoneNumber.Replace(Convert.ToChar(45).ToString(), "").Replace(Convert.ToChar(95).ToString(), "");
            // Si la longitud no es la adecuada, no formateo nada
            if (phoneNumber.Length % format.Replace(" ", "").Length != 0) return initialPhoneNumber;

            while (posChar <= phoneNumber.Length - 1)
            {
                while (posFmt <= format.Length - 1)
                {
                    if (format.Substring(posFmt, 1) == "#")
                    {
                        formatedString = formatedString + phoneNumber.Substring(posChar, 1);
                        posChar++;
                    }
                    else
                    {
                        formatedString = formatedString + format.Substring(posFmt, 1);
                    }
                    posFmt++;
                }
                posFmt = 1;
                formatedString = formatedString + " " + separator + " ";
            };
            return formatedString.Substring(0, formatedString.Length - (separator.Length + 2));
        }

        /// <summary>
        /// Implementacion del Right sobre un String
        /// </summary>
        /// <param name="value">Cadena base</param>
        /// <param name="length">Cantidad de caracteres a retornar</param>
        /// <returns></returns>
        public static string Right(this string value, int length)
        {
            return value.Substring(value.Length - length);
        }

        /// <summary>
        /// Retorna el digito deverificacion para un Nit
        /// </summary>
        /// <param name="nit">Nit Sin Digito de Verificacion</param>
        /// <returns></returns>
        public static string GetVerificationDigit(this string nit)
        {
            int[] arrayPA = { 71, 67, 59, 53, 47, 43, 41, 37, 29, 23, 19, 17, 13, 7, 3 };
            string wDato = string.Concat(new string('0', 15), nit.Trim()).Right(15);
            int wSuma = 0;
            for (int i = 0; i < 15; i++)
            {
                string s = wDato.Substring(i, 1);
                // int t = int.Parse(s);
                wSuma += int.Parse(wDato.Substring(i, 1)) * arrayPA[i];
            }
            wSuma %= 11;
            return wSuma == 0 || wSuma == 1 ? wSuma.ToString().Trim() : (11 - wSuma).ToString().Trim();
        }

        public static string GetExceptionDetails(this Exception exception)
        {
            var properties = exception.GetType()
                                    .GetProperties();
            var fields = properties
                             .Select(property => new
                             {
                                 Name = property.Name,
                                 Value = property.GetValue(exception, null)
                             })
                             .Select(x => String.Format(
                                 "{0} = {1}",
                                 x.Name,
                                 x.Value != null ? x.Value.ToString() : String.Empty
                             ));
            return String.Join("\n", fields);
        }
    }
}
