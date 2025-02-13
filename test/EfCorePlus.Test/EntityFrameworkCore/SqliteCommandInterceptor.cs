using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics;

namespace EfCorePlus.Test.EntityFrameworkCore
{
    internal class SqliteCommandInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Debug.WriteLine(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }
    }
}
