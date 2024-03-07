using System;

using System.Text;
using com.nem.aurawheel.Modelo;
using System.Collections;
using System.Collections.Generic;
using com.nem.aurawheel.Protocolo;
using com.nem.aurawheel.Rest.Json;

namespace com.nem.aurawheel.Utils.Rest
{
    class ParseJson
    {
        public static UpgradeJson ToUpgradeJson(Hashtable upgradeJson)
        {
            return new UpgradeJson((string)upgradeJson["p_cv"],
                                   (string)upgradeJson["p_nv"],
                                   (string)upgradeJson["p_ur"],
                                   ((string)upgradeJson["p_m5"]).Replace("\t", ""));
        }

        public static OrdenTrabajo ToOrdenTrabajo(Hashtable workOrder)
        {
            OrdenTrabajo ordenTrabajo = new OrdenTrabajo();
            ordenTrabajo.ID = Convert.ToInt32(workOrder["wo_id"]);
            ordenTrabajo.ID_Flota = Convert.ToInt32(workOrder["wo_fl"]);
            ordenTrabajo.ID_Unidad = Convert.ToInt32(workOrder["wo_un"]);
            ordenTrabajo.ID_Operario = Convert.ToInt32(workOrder["wo_em"]);
            // fecha de OT "yyyy-MM-dd"
            ordenTrabajo.Fecha = NemDateUtils.strDateToDatetime(Convert.ToString(workOrder["wo_ts"]));
            ordenTrabajo.Estado = 0;
            //ordenTrabajo.FechaUltima;
            //ordenTrabajo.NumMedidas;
            //ordenTrabajo.NumNoMedidas;
            ordenTrabajo.Tipo = Convert.ToString(workOrder["wo_ty"]);
            return ordenTrabajo;
        }

        public static Operario ToOperario(Hashtable employer)
        {
            Operario operario = new Operario();
            operario.ID = Convert.ToInt32(employer["em_id"]);
            operario.Nombre = (string)employer["em_n"];
            operario.Password = (string)employer["em_p"];

            operario.Apellido1 = "";
            operario.Apellido2 = "";
            operario.Idioma = "";
            return operario;
        }

        public static Flota ToFlota(Hashtable fleet)
        {
            Flota flota = new Flota();
            flota.ID = Convert.ToInt32(fleet["fl_id"]);
            flota.L1 = Convert.ToInt32(fleet["fl_l1"]);
            flota.L2 = Convert.ToInt32(fleet["fl_l2"]);
            flota.L3 = Convert.ToInt32(fleet["fl_l3"]);
            flota.Referencia = (string)fleet["fl_rf"];
            flota.ID_PerfRef = Convert.ToInt32(fleet["fl_rp"]);

            flota.Nombre = "";
            flota.RefRuedaDcha = "";
            flota.RefRuedaIzda = "";
            return flota;
        }

        public static Unidad ToUnidad(Hashtable unit, Flota flota)
        {
            Unidad unidad = new Unidad();
            unidad.Referencia = (string)unit["un_rf"];
            unidad.ID = Convert.ToInt32(unit["un_id"]);
            unidad.ID_Flota = flota.ID;
            
            unidad.Km = 0;
            unidad.Actualizada = false;
            unidad.Descripcion = "";
            unidad.Nombre = "";
            return unidad;
        }

        public static Coche ToCoche(Hashtable car, Unidad unidad)
        {
            Coche coche = new Coche();
            coche.Referencia = (string)car["ca_rf"];
            coche.ID = Convert.ToInt32(car["ca_id"]);
            coche.ID_Flota = unidad.ID_Flota;
            coche.ID_Unidad = unidad.ID;

            coche.Descripcion = "";
            coche.Nombre = "";
            return coche;
        }

        public static Bogie ToBogie(Hashtable bogie, Coche coche)
        {
            Bogie _bogie = new Bogie();
            _bogie.Referencia = (string)bogie["bo_rf"];
            _bogie.ID = Convert.ToInt32(bogie["bo_id"]);
            _bogie.ID_Coche = coche.ID;
            _bogie.ID_Flota = coche.ID_Flota;
            _bogie.ID_Unidad = coche.ID_Unidad;

            _bogie.Posicion = Convert.ToInt32(bogie["bo_p"]);
            _bogie.Descripcion = "";
            return _bogie;
        }

        public static Eje ToEje(Hashtable axle, Bogie bogie)
        {
            Eje eje = new Eje();
            eje.ID = Convert.ToInt32(axle["ax_id"]);
            eje.Referencia = (string)axle["ax_rf"];
            eje.ID_Bogie = bogie.ID;
            eje.ID_Coche = bogie.ID_Coche;
            eje.ID_Flota = bogie.ID_Flota;
            eje.ID_Unidad = bogie.ID_Unidad;

            eje.Posicion = Convert.ToInt32(axle["ax_p"]);
            eje.Descripcion = "";
            return eje;
        }

        public static Componente ToRueda(Hashtable wheel, Eje eje)
        {
            Componente rueda = new Componente();
            rueda.ID = Convert.ToInt32(wheel["wh_id"]);
            rueda.ID_Bogie = eje.ID_Bogie;
            rueda.ID_Coche = eje.ID_Coche;
            rueda.ID_Eje = eje.ID;
            rueda.ID_Flota = eje.ID_Flota;
            rueda.ID_Unidad = eje.ID_Unidad;
            rueda.IsIzquierda = Convert.ToInt32(wheel["wh_n"])==0;

            rueda.Nombre = "";
            rueda.Referencia = "";
            rueda.Tipo = "";
            return rueda;
        }

        public static PerfilReferencia ToPerfilReferencia(Hashtable json, ProfileParameters parameters)
        {
            PerfilReferencia reference = new PerfilReferencia();
            reference.ID = Convert.ToInt32(json["p_id"]);
            reference.Descripcion = (string)json["p_dc"];
            reference.Nombre = (string)json["p_nm"];
            reference.SetProfile(ToProf(json, parameters));
            //reference.ID_Flota;
            return reference;
        }

        public static string ToProf(Hashtable json, ProfileParameters parameters)
        {
            string prof = DateTime.Now.ToString() + "\n";
            prof += "H=0,G=0,qR=0\n";
            prof += String.Format("L1={0},L2={1},L3={2}\n", parameters.L1, parameters.L2, parameters.L3);
            for (int i = 0; i < 9; i++) prof += "#\n";
            ArrayList points = (ArrayList)json["p_pr"];
            for (int i = 0; i < points.Count; i++)
            {
                ArrayList point = (ArrayList)points[i];
                prof += String.Format("{0},{1}\n", point[0], point[1]);
            }
            return prof;
        }

        public static ArrayList FromProfile(Profile profile)
        {
            List<double> x = profile.baseX;
            List<double> y = profile.baseY;
            ArrayList points = new ArrayList();
            for (int i = 0; i < x.Count; i++)
            {
                ArrayList point = new ArrayList();
                point.Add(x[i]);
                point.Add(y[i]);
                points.Add(point);
            }
            return points;
        }

    }
}
