using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

public class ConfigValidator
{
	private readonly IConfiguration _Configuration;
	private readonly ILogger _Logger;
	//private readonly IHostEnvironment _Environment;
	private readonly List<string> _Errors = new ();
	
	public ConfigValidator(IConfiguration configuration, ILogger logger)
	{
		this._Configuration = configuration;
		this._Logger = logger;
	}

	#region Validation

	public ConfigValidator Validate()
	{
		if (this._Errors.Count == 0)
		{
			this._Logger.LogInformation("Configuration validation passed correctly.");
			return this;
		}
		string summary = "Configuration validation failed with the following errors:\n" + string.Join("\n", this._Errors.Select(e => $"  - {e}.")) + "\nCreate or fix the corresponding settings inside 'appsettings.json' file.";
		this._Logger.LogCritical(summary);
		return this;
	}

	public ConfigValidator RequireValue<T>(string key, Action<T>? extraValidation = null)
	{
		T? value = this._Configuration.GetValue<T>(key);
		if (value is null || (value is string str && string.IsNullOrEmpty(str)))
		{
			_Errors.Add($"Configuration value for '{key}' is missing or empty.");
		}
		else
		{
			extraValidation?.Invoke(value);
		}
		return this;
	}

	public ConfigValidator RequireConnectionString(string name)
	{
		string? connectionString = this._Configuration.GetConnectionString(name);
		if (string.IsNullOrEmpty(connectionString))
		{
			_Errors.Add($"Connection string to database with name '{name}' is missing or empty.");
		}
		return this;
	}

	public ConfigValidator RequireSection(string sectionKey, Action<ConfigValidator>? sectionValidations = null)
	{
		IConfigurationSection section = this._Configuration.GetSection(sectionKey);
		if (!section.Exists())
		{
			_Errors.Add($"Configuration section '{sectionKey}' is missing.");
		}
		if (sectionValidations is not null)
		{
			ConfigValidator sectionValidator = new ConfigValidator(section, this._Logger);
			sectionValidations(sectionValidator);
			this._Errors.AddRange(sectionValidator._Errors.Select(e => $"In section '{sectionKey}': {e}."));
		}
		return this;
	}

	public ConfigValidator RequireUrl(string key)
	{
		return RequireValue<string>(key, value =>
		{
			if (!Uri.TryCreate(value, UriKind.Absolute, out _))
			{
				this._Errors.Add($"The key {key} with value '{value}' is not a valid URL.");
			}
		});
	}

	public ConfigValidator RequireMinLength(string key, int min)
	{
		return RequireValue<string>(key, value =>
		{
			if (value.Length < min)
			{
				this._Errors.Add($"The key {key} with value '{value}' must be {min} characters long.");
			}
		});
	}

	public ConfigValidator CheckIf(bool condition, Action<ConfigValidator> validations)
	{
		if (condition)
		{
			validations(this);
		}
		return this;
	}

	#endregion

	public T? GetValue<T>(string key)
	{
		T? value = this._Configuration.GetValue<T>(key);
		if (value == null || (value is string str && string.IsNullOrEmpty(str)))
		{
			throw new InvalidOperationException($"Configuration value for '{key}' is missing or empty.");
		}
		return value;
	}

	public string GetConnectionString(string name)
	{
		string? connectionString = this._Configuration.GetConnectionString(name);
		if (string.IsNullOrEmpty(connectionString))
		{
			throw new InvalidOperationException($"Connection string to database with name '{name}' is missing or empty.");
		}
		return connectionString;
	}

	public IConfigurationSection GetSection(string sectionKey)
	{
		IConfigurationSection section = this._Configuration.GetSection(sectionKey);
		if (!section.Exists())
		{
			throw new InvalidOperationException($"Configuration section '{sectionKey}' is missing.");
		}
		return section;
	}
}
