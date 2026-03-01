using AduanaDigital.Models;
using Dapper;
using System.Data;

namespace AduanaDigital.Data
{
    public class ProductoImportacionRepository
    {
        public async Task<int> InsertProductoImportacion(ProductoImportacion producto)
        {
            int nuevoId = 0;

            using (var cxn = ConnectionFactory.GetConnection)
            {
                var query = "sp_InsertarProductoImportacion";
                var parametros = new DynamicParameters();
                
                parametros.Add("@codigo", producto.Codigo);
                parametros.Add("@foto_url", producto.FotoUrl);
                parametros.Add("@nombre_chino", producto.NombreChino);
                parametros.Add("@pcs_por_caja", producto.PcsPorCaja);
                parametros.Add("@cbm_por_caja", producto.CbmPorCaja);
                parametros.Add("@explicacion", producto.Explicacion);
                parametros.Add("@tienda_contacto", producto.TiendaContacto);
                parametros.Add("@nombre_espanol", producto.NombreEspanol);
                parametros.Add("@aut_sant", producto.AutSant);
                parametros.Add("@total_cajas", producto.TotalCajas);
                parametros.Add("@total_cbm", producto.TotalCbm);
                parametros.Add("@total_rmb", producto.TotalRmb);

                // ExecuteScalarAsync captura el valor retornado por SELECT SCOPE_IDENTITY()
                var resultado = await cxn.ExecuteScalarAsync<int>(
                    query, 
                    param: parametros, 
                    commandType: CommandType.StoredProcedure
                );

                nuevoId = resultado;
            }

            return nuevoId;
        }
    }
}