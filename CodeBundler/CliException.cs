using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace CodeBundler;

/// <summary>
/// שגיאה צפויה שניתן להציג למשתמש בצורה ידידותית.
/// </summary>
internal sealed class CliException(string message) : Exception(message);
