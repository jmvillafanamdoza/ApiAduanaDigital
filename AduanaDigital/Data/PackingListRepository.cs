using AduanaDigital.Models;
using Dapper;
using System.Data;

namespace AduanaDigital.Data
{
    public class PackingListRepository
    {
        /// <summary>
        /// Crea la cabecera en TADI_PACKING_LIST y retorna el ID generado
        /// </summary>
        public async Task<int> CrearPackingList(string nombreArchivo)
        {
            using var cxn = ConnectionFactory.GetConnection;

            var parametros = new DynamicParameters();
            parametros.Add("@nom_archivo", nombreArchivo);

            return await cxn.ExecuteScalarAsync<int>(
                "sp_CrearPackingList",
                parametros,
                commandType: CommandType.StoredProcedure
            );
        }

        /// <summary>
        /// Inserta una fila en TADI_PACKING_LIST_DETALLE y retorna el ID generado
        /// </summary>
        public async Task<int> InsertarDetalle(PackingListDetalle detalle)
        {
            using var cxn = ConnectionFactory.GetConnection;

            var parametros = new DynamicParameters();
            parametros.Add("@id_packing_list", detalle.PackingListId);
            parametros.Add("@codigo", detalle.Codigo);
            parametros.Add("@foto_url", detalle.FotoUrl);
            parametros.Add("@nombre_chino", detalle.NombreChino);
            parametros.Add("@pcs_por_caja", detalle.PcsPorCaja);
            parametros.Add("@cbm_por_caja", detalle.CbmPorCaja);
            parametros.Add("@explicacion", detalle.Explicacion);
            parametros.Add("@tienda_contacto", detalle.TiendaContacto);
            parametros.Add("@nombre_espanol", detalle.NombreEspanol);
            parametros.Add("@aut_sant", detalle.AutSant);
            parametros.Add("@total_cajas", detalle.TotalCajas);
            parametros.Add("@total_cbm", detalle.TotalCbm);
            parametros.Add("@total_rmb", detalle.TotalRmb);

            return await cxn.ExecuteScalarAsync<int>(
                "sp_InsertarPackingListDetalle",
                parametros,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}