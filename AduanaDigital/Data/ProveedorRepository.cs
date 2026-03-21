using AduanaDigital.Models;
using Dapper;
using System.Data;

namespace AduanaDigital.Data
{
    public class ProveedorRepository
    {
        public async Task<IEnumerable<Proveedor>> ObtenerProveedores()
        {
            using var cxn = ConnectionFactory.GetConnection;
            return await cxn.QueryAsync<Proveedor>(
                "sp_ObtenerProveedores",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<Proveedor?> InsertarProveedor(CrearProveedorRequest request)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@nombre", request.Nombre);
            p.Add("@contacto", request.Contacto);
            p.Add("@tipoDocumento", request.TipoDocumento);
            p.Add("@numeroDocumento", request.NumeroDocumento);
            p.Add("@celular", request.Celular);
            p.Add("@correo", request.Correo);
            p.Add("@paisOrigen", request.PaisOrigen);
            p.Add("@direccion", request.Direccion);
            return await cxn.QueryFirstOrDefaultAsync<Proveedor>(
                "sp_InsertarProveedor", p, commandType: CommandType.StoredProcedure);
        }
    }
}