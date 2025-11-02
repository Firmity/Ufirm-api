using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

public static class PayrollCalculator
{
    /// <summary>
    /// Evaluates a formula string with variables replaced by their values.
    /// Example: "BaseSalary * 0.2 + HouseAllowance"
    /// </summary>
    public static decimal EvaluateFormula(string formula, Dictionary<string, decimal> variables)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return 0;

        // Replace variable names in the formula with their values
        foreach (var kvp in variables)
        {
            // Use word boundary to avoid partial replacements (e.g., Base and BaseSalary)
            formula = Regex.Replace(formula, $@"\b{kvp.Key}\b", kvp.Value.ToString(), RegexOptions.IgnoreCase);
        }

        // Use DataTable.Compute to calculate the result
        var dt = new DataTable();
        var value = dt.Compute(formula, "");

        return Convert.ToDecimal(value);
    }

    /// <summary>
    /// Calculates multiple formulas at once.
    /// </summary>
    public static Dictionary<string, decimal> Calculate(Dictionary<string, decimal> variables, Dictionary<string, string> formulas)
    {
        var results = new Dictionary<string, decimal>();

        foreach (var kvp in formulas)
        {
            var name = kvp.Key;
            var formula = kvp.Value;

            var result = EvaluateFormula(formula, variables);

            results[name] = result;

            // Add to variables so other formulas can use it
            if (!variables.ContainsKey(name))
                variables[name] = result;
            else
                variables[name] = result;
        }

        return results;
    }
}
