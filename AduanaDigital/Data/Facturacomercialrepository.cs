using AduanaDigital.Models;
using Dapper;
using System.Data;

namespace AduanaDigital.Data
{
    public class FacturaComercialRepository
    {
        // ─────────────────────────────────────────────
        // CLIENTES
        // ─────────────────────────────────────────────
        public async Task<IEnumerable<Cliente>> ObtenerClientes()
        {
            using var cxn = ConnectionFactory.GetConnection;
            return await cxn.QueryAsync<Cliente>(
                "sp_ObtenerClientes", commandType: CommandType.StoredProcedure);
        }

        public async Task<Cliente?> InsertarCliente(CrearClienteRequest request)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@nombre", request.Nombre);
            p.Add("@contacto", request.Contacto);
            p.Add("@tipoDocumento", request.TipoDocumento);
            p.Add("@numeroDocumento", request.NumeroDocumento);
            p.Add("@celular", request.Celular);
            p.Add("@correo", request.Correo);
            return await cxn.QueryFirstOrDefaultAsync<Cliente>(
                "sp_InsertarCliente", p, commandType: CommandType.StoredProcedure);
        }

        // ─────────────────────────────────────────────
        // PACKING LIST
        // ─────────────────────────────────────────────
        public async Task AsociarClientePackingList(int clienteId, int packingListId, string? observacion = null)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@id_cliente", clienteId);
            p.Add("@id_packing_list", packingListId);
            p.Add("@observacion", observacion);
            await cxn.ExecuteAsync(
                "sp_AsociarClientePackingList", p, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PackingListResumen>> ObtenerPackingListsPorCliente(int clienteId)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@id_cliente", clienteId);
            return await cxn.QueryAsync<PackingListResumen>(
                "sp_ObtenerPackingListsPorCliente", p, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PackingListItemAgrupado>> ObtenerItemsPackingListAgrupados(int packingListId)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@id_packing_list", packingListId);
            return await cxn.QueryAsync<PackingListItemAgrupado>(
                "sp_ObtenerItemsPackingListAgrupados", p, commandType: CommandType.StoredProcedure);
        }

        // ─────────────────────────────────────────────
        // FACTURAS COMERCIALES
        // ─────────────────────────────────────────────
        public async Task<IEnumerable<FacturaComercial>> ObtenerFacturasComerciales()
        {
            using var cxn = ConnectionFactory.GetConnection;
            using var multi = await cxn.QueryMultipleAsync(
                "sp_ObtenerFacturasComerciales", commandType: CommandType.StoredProcedure);
            var cabeceras = (await multi.ReadAsync<FacturaComercial>()).ToList();
            var items = (await multi.ReadAsync<FacturaDetalleRow>()).ToList();
            foreach (var f in cabeceras)
                f.Items = items.Where(i => i.FacturaId == f.Id).ToList();
            return cabeceras;
        }

        public async Task<IEnumerable<FacturaComercial>> ObtenerFacturasPorCliente(int clienteId)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@id_cliente", clienteId);

            // SP retorna 2 result sets: cabeceras + items
            using var multi = await cxn.QueryMultipleAsync(
                "sp_ObtenerFacturasPorCliente", p, commandType: CommandType.StoredProcedure);

            var cabeceras = (await multi.ReadAsync<FacturaComercial>()).ToList();
            var items = (await multi.ReadAsync<FacturaDetalleRow>()).ToList();

            foreach (var f in cabeceras)
                f.Items = items.Where(i => i.FacturaId == f.Id).ToList();

            return cabeceras;
        }

        public async Task<FacturaComercial?> GenerarFacturaDesdePackingList(GenerarFacturaRequest request)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@id_cliente", request.ClienteId);
            p.Add("@id_proveedor", request.ProveedorId);
            p.Add("@id_packing_list", request.PackingListId);
            p.Add("@fecha", request.Fecha);

            using var multi = await cxn.QueryMultipleAsync(
                "sp_GenerarFacturaDesdePackingList", p, commandType: CommandType.StoredProcedure);

            var factura = await multi.ReadFirstOrDefaultAsync<FacturaComercial>();
            if (factura != null)
                factura.Items = (await multi.ReadAsync<FacturaDetalleRow>()).ToList();

            return factura;
        }

        public async Task<int> InsertarDetalleFactura(FacturaDetalleRow item)
        {
            using var cxn = ConnectionFactory.GetConnection;
            var p = new DynamicParameters();
            p.Add("@id_factura_comercial", item.FacturaId);
            p.Add("@codigo", item.Codigo);
            p.Add("@nombre_espanol", item.Description);
            p.Add("@aut_sant", item.AutSant);
            p.Add("@ctns", item.Ctns);
            p.Add("@pcs", item.Pcs);
            p.Add("@unit_price", item.UnitPrice);
            p.Add("@foto_url", item.ImagenUrl);
            return await cxn.ExecuteScalarAsync<int>(
                "sp_InsertarDetalleFacturaComercial", p, commandType: CommandType.StoredProcedure);
        }
    }
}