namespace GameBusiness.Shop.UI
{
    using GameBusiness.Wallet.Model;
    using UnityEngine;

    public static class CurrenciesHelper
    {
        private static readonly string[] Digits = new string[] { "", "K", "M", "B", "T", "Qa" };

        public static string ValueFormatted(this Currency currency)
        {
            return NumberFormat(currency.Value);
        }

        public static string NumberFormat(this int value)
        {
            return NumberFormat((float)value);
        }
        
        public static string NumberFormat(this float value)
        {
            float moneyRepresentation = value;
            int counter = 0;

            while (moneyRepresentation >= 1000)
            {
                moneyRepresentation /= 1000;
                counter++;
            }

            if (moneyRepresentation >= 100)
            {
                moneyRepresentation = Mathf.Floor(moneyRepresentation);
                if (counter != 0)
                    return moneyRepresentation.ToString("F0") + GetDigits(counter);
            }
            else if (moneyRepresentation >= 10)
            {
                string result = moneyRepresentation.ToString("F1");

                if (result[result.Length - 1] == '0')
                    result = result.Remove(result.Length - 2);

                if (counter != 0)
                    return result + GetDigits(counter);
            }
            else
            {
                string result = moneyRepresentation.ToString("F2");

                if (result[result.Length - 1] == '0')
                {
                    result = result.Remove(result.Length - 1);

                    if (result[result.Length - 1] == '0')
                        result = result.Remove(result.Length - 2);
                }

                if (counter != 0)
                    return result + GetDigits(counter);
            }

            return Mathf.RoundToInt(moneyRepresentation).ToString();
        }

        private static string GetDigits(int index)
        {
            if (index < 0 || index >= Digits.Length)
                return "";

            return Digits[index];
        }
    }
}