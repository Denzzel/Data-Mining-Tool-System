﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dms.models;
using dms.services.preprocessing.normalization;
using dms.view_models;

namespace dms.services.preprocessing
{
    class Preprocessing
    {
        private static Preprocessing preprocessing;
        public static Preprocessing PreprocessingObj
        {
            get
            {
                if (preprocessing == null)
                {
                    preprocessing = new Preprocessing();

                }
                return preprocessing;
            }
        }

        public Preprocessing()
        { }

        public int addNewEntitiesForPreprocessing(string selectionName, int countRows, int taskTemplateId)
        {
            DataHelper helper = new DataHelper();

            string type = "develop";
            int selectionId = helper.addSelection(selectionName, taskTemplateId, countRows, type);

            List<Entity> listSelRow = new List<Entity>(countRows);
            for (int i = 0; i < countRows; i++)
            {
                SelectionRow entity = helper.addSelectionRow(selectionId, i + 1);
                listSelRow.Add(entity);
            }
            DatabaseManager.SharedManager.insertMultipleEntities(listSelRow);

            return selectionId;
        }
        
        private List<int> pars = new List<int>();
        public IParameter executePreprocessing(int newSelectionId, int oldSelectionId, int oldParamId, string prepType, int parameterPosition, int newParamId)
        {
            models.Parameter oldParam = ((models.Parameter)DatabaseManager.SharedManager.entityById(oldParamId, typeof(models.Parameter)));
            TypeParameter type;
            switch (prepType)
            {
                case "Линейная нормализация 1 (к float)":
                    type = TypeParameter.Real;
                    break;
                case "Нелинейная нормализация 2 (к float)":
                    type = TypeParameter.Real;
                    break;
                case "нормализация 3 (к int)":
                    type = TypeParameter.Int;
                    break;
                case "бинаризация":
                    type = TypeParameter.Int;
                    break;
                case "без предобработки":
                    type = oldParam.Type;
                    break;
                default:
                    type = TypeParameter.Real;
                    break;
            }
            
            List<string> values = new List<string>();
            List<Entity> valueParam = new List<Entity>();
            
            List<Entity> oldSelectionRows = SelectionRow.where(new Query("SelectionRow").addTypeQuery(TypeQuery.select)
                .addCondition("SelectionID", "=", oldSelectionId.ToString()), typeof(SelectionRow));

            int index = 0;
            foreach (Entity entity in oldSelectionRows)
            {
                int selectionRowId = entity.ID;
                List<Entity> list = ValueParameter.where(new Query("ValueParameter").addTypeQuery(TypeQuery.select)
                .addCondition("ParameterID", "=", oldParamId.ToString()).
                addCondition("SelectionRowID", "=", selectionRowId.ToString()), typeof(ValueParameter));
                valueParam = valueParam.Concat(list).ToList();
                values.Add(((ValueParameter)valueParam[index]).Value);
                index++;
            }

            IParameter p = null;
            switch (prepType)
            {
                case "Линейная нормализация 1 (к float)":
                case "Нелинейная нормализация 2 (к float)":
                case "нормализация 3 (к int)":
                    if (oldParam.Type == TypeParameter.Real)
                    {
                        p = new RealParameter(values);
                        normalizeValues(valueParam, p, newParamId, newSelectionId, prepType);
                    }
                    else if (oldParam.Type == TypeParameter.Int)
                    {
                        p = new IntegerParameter(values);
                        normalizeValues(valueParam, p, newParamId, newSelectionId, prepType);
                    }
                    else if (oldParam.Type == TypeParameter.Enum)
                    {
                        p = new EnumeratedParameter(values);
                        normalizeValues(valueParam, p, newParamId, newSelectionId, prepType);
                    }
                    break;
                case "бинаризация":
                    binarizationValues(valueParam, newParamId, newSelectionId, parameterPosition);
                    break;
                case "без предобработки":
                    processWithoutPreprocessing(valueParam, newParamId, newSelectionId);
                    break;
            }
            return p;
        }
        
        private void processWithoutPreprocessing(List<Entity> values, int paramId, int newSelectionId)
        {
            DataHelper helper = new DataHelper();
            List<Entity> selectionRows = SelectionRow.where(new Query("SelectionRow").addTypeQuery(TypeQuery.select)
                .addCondition("SelectionID", "=", newSelectionId.ToString()), typeof(SelectionRow));

            int index = 0;
            List<Entity> listValues = new List<Entity>();
            foreach (Entity value in values)
            {
                string val = withoutPreprocessing(value);
                listValues.Add(helper.addValueParameter(selectionRows[index].ID, paramId, val));
                index++;
            }
            DatabaseManager.SharedManager.insertMultipleEntities(listValues);
        }

        private void binarizationValues(List<Entity> values, int paramId, int newSelectionId, int parameterPosition)
        {
            DataHelper helper = new DataHelper();
            List<Entity> selectionRows = SelectionRow.where(new Query("SelectionRow").addTypeQuery(TypeQuery.select)
                .addCondition("SelectionID", "=", newSelectionId.ToString()), typeof(SelectionRow));

            List<Entity> listValues = new List<Entity>();

            List<string> valueStr = new List<string>();
            foreach (Entity value in values)
            {
                valueStr.Add(((ValueParameter)value).Value);
            }
            EnumeratedParameter p = new EnumeratedParameter(valueStr);

            int index = 0;
            foreach (string value in valueStr)
            {
                int i = p.GetInt(value);
                string val = binarization(parameterPosition == i);
                listValues.Add(helper.addValueParameter(selectionRows[index].ID, paramId, val));
                index++;
            }
            DatabaseManager.SharedManager.insertMultipleEntities(listValues);
        }

        private void normalizeValues(List<Entity> values, IParameter p, int paramId, int newSelectionId, string prepType)
        {
            List<Entity> listValues = new List<Entity>();
            DataHelper helper = new DataHelper();
            List<Entity> selectionRows = SelectionRow.where(new Query("SelectionRow").addTypeQuery(TypeQuery.select)
                .addCondition("SelectionID", "=", newSelectionId.ToString()), typeof(SelectionRow));

            int index = 0;
            foreach (Entity value in values)
            {
                string val;
                switch (prepType)
                {
                    case "Линейная нормализация 1 (к float)":
                        val = normalize(1, value, p);
                        break;
                    case "Нелинейная нормализация 2 (к float)":
                        val = normalize(2, value, p);
                        break;
                    case "нормализация 3 (к int)":
                        val = normalize(3, value, p);
                        break;
                    default:
                        val = "";
                        break;
                }
                listValues.Add(helper.addValueParameter(selectionRows[index].ID, paramId, val));
                index++;
            }
            DatabaseManager.SharedManager.insertMultipleEntities(listValues);
        }

        private string normalize(int type, Entity value, IParameter p)
        {
            if (type == 1)
            {
                return p.GetLinearNormalizedFloat(((ValueParameter)value).Value).ToString();
            } else if (type == 2)
            {
                return p.GetNonlinearNormalizedFloat(((ValueParameter)value).Value).ToString();
            } else
            {
               // return "0";
                return p.GetNormalizedInt(((ValueParameter)value).Value).ToString();
            }
        }

        private string binarization(bool flag)
        {
            if (flag)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }

        private string withoutPreprocessing(Entity value)
        {
            return ((ValueParameter)value).Value;
        }

        public List<string> getAppropriateValues(List<string> obtainedValues, int selectionId, int parameterId)
        {
            //
     //       float valueDec = Convert.ToSingle(value.Replace(".", ","));
            //Формируем выборку для заданного параметра
            List<Entity> selectionRows = SelectionRow.where(new Query("SelectionRow").addTypeQuery(TypeQuery.select)
                .addCondition("SelectionID", "=", selectionId.ToString()), typeof(SelectionRow));

            List<float> valuesForCurrParameter = new List<float>();
            foreach (Entity selRow in selectionRows)
            {
                int selectionRowId = selRow.ID;
                List<Entity> valueForParamFromRow = ValueParameter.where(new Query("ValueParameter").addTypeQuery(TypeQuery.select)
                        .addCondition("ParameterID", "=", parameterId.ToString())
                        .addCondition("SelectionRowID", "=", selectionRowId.ToString()), typeof(ValueParameter));

                string numberStr = ((ValueParameter)valueForParamFromRow[0]).Value;
                float number = Convert.ToSingle(numberStr.Replace(".", ","));
                valuesForCurrParameter.Add(number);
            }
            valuesForCurrParameter.Sort();
            //находим в выборке соответсвующее значение для value (переданного аргумента) и присваиваем его appropriateValue
            float step = 0;
            List<string> appropriateValues = new List<string>();
            
            for (int j = 0; j < obtainedValues.Count; j++)
            {
                float obtainedValue = Convert.ToSingle(obtainedValues[j].Replace(".", ","));

                float prev = valuesForCurrParameter[0];
                for (int i = 1; i < valuesForCurrParameter.Count; i++)
                {
                    float next = valuesForCurrParameter[i];
                    step = Math.Abs(next - prev);
                    if ((obtainedValue - prev) <= (step / 2))
                    {
                        appropriateValues[j] = prev.ToString();
                        break;
                    }
                    prev = next;
                }
                //проверка на выод за границу диапозона значений в выборке ???
                if (appropriateValues[j].Equals(""))
                {
                    float firstVal = valuesForCurrParameter[0];
                    float lastVal = valuesForCurrParameter[valuesForCurrParameter.Count - 1];
                    if (obtainedValue >= lastVal)
                    {
                        appropriateValues[j] = lastVal.ToString();
                    }
                    else if (obtainedValue <= firstVal)
                    {
                        appropriateValues[j] = firstVal.ToString();
                    }
                }
            }
            return appropriateValues;
        }

        public List<bool> getResults(int selectionId, int parameterId, List<string> appropriateValues, List<string> expectedValues)
        {
            //оперделяем шаблон для выборки и достаем из него PreprocessingParameters 
            Selection selection = ((Selection)services.DatabaseManager.SharedManager.entityById(selectionId, typeof(Selection)));
            int templateId = selection.TaskTemplateID;
            TaskTemplate template = ((TaskTemplate)services.DatabaseManager.SharedManager.entityById(templateId, typeof(TaskTemplate)));
            PreprocessingViewModel.PreprocessingTemplate prepParameters = (PreprocessingViewModel.PreprocessingTemplate)template.PreprocessingParameters;
            List<PreprocessingViewModel.SerializableList> info = prepParameters.info;
            List<view_models.Parameter> parametersWithPrepType = prepParameters.parameters;
            //находим нужный preprocessing list и нужное преобразование
            foreach (PreprocessingViewModel.SerializableList elem in info)
            {
                if (selectionId.Equals(elem.selectionId))
                {
                    List<int> parameterIdList = elem.parameterIds;
                    int index = 0;
                    foreach (int paramId in parameterIdList)
                    {
                        if (parameterId.Equals(paramId))
                        {
                            IParameter p = elem.prepParameters[index];
                            foreach (view_models.Parameter prepParam in parametersWithPrepType)
                            {
                                if (parameterId.Equals(prepParam.Id))
                                {
                                    string prepType = prepParam.Type;
                                    switch (prepType)
                                    {
                                        case "Линейная нормализация 1 (к float)":
                                            return getValuesFromLinearNormalized(appropriateValues, expectedValues, p);
                                          //  result = p.GetFromLinearNormalized(Convert.ToSingle(appropriateValue.Replace(".", ",")));
                                          //  break;
                                        case "Нелинейная нормализация 2 (к float)":
                                            return getValuesFromNonlinearNormalized(appropriateValues, expectedValues, p);
                                        case "нормализация 3 (к int)":
                                            return getValuesFromNormalized(appropriateValues, expectedValues, p);
                                        case "бинаризация":
                                            //добавить бинаризацию!!!!
                                            break;
                                        case "без предобработки":
                                            return getValuesWithoutPreprocessing(appropriateValues, expectedValues);
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                        index++;
                    }
                    break;
                }
            }
            return null;
        }

        public List<bool> getValuesFromLinearNormalized(List<string> appropriateValues, List<string> expectedValues, IParameter p)
        {
            List<bool> results = new List<bool>();
            for (int i = 0; i < appropriateValues.Count; i++)
            {
                string apVal = p.GetFromLinearNormalized(Convert.ToSingle(appropriateValues[i].Replace(".", ",")));
                string exVal = p.GetFromLinearNormalized(Convert.ToSingle(expectedValues[i].Replace(".", ",")));

                if (!exVal.Equals("") && !apVal.Equals("") && exVal.Equals(apVal))
                {
                    results.Add(true);
                }
                else
                {
                    results.Add(false);
                }
            }
            return results;
        }

        public List<bool> getValuesFromNonlinearNormalized(List<string> appropriateValues, List<string> expectedValues, IParameter p)
        {
            List<bool> results = new List<bool>();
            for (int i = 0; i < appropriateValues.Count; i++)
            {
                string apVal = p.GetFromNonlinearNormalized(Convert.ToSingle(appropriateValues[i].Replace(".", ",")));
                string exVal = p.GetFromNonlinearNormalized(Convert.ToSingle(expectedValues[i].Replace(".", ",")));

                if (!exVal.Equals("") && !apVal.Equals("") && exVal.Equals(apVal))
                {
                    results.Add(true);
                }
                else
                {
                    results.Add(false);
                }
            }
            return results;
        }

        public List<bool> getValuesFromNormalized(List<string> appropriateValues, List<string> expectedValues, IParameter p)
        {
            List<bool> results = new List<bool>();
            for (int i = 0; i < appropriateValues.Count; i++)
            {
                string apVal = p.GetFromNormalized(Convert.ToInt32(appropriateValues[i]));
                string exVal = p.GetFromNormalized(Convert.ToInt32(expectedValues[i]));

                if (!exVal.Equals("") && !apVal.Equals("") && exVal.Equals(apVal))
                {
                    results.Add(true);
                }
                else
                {
                    results.Add(false);
                }
            }
            return results;
        }

        public List<bool> getValuesWithoutPreprocessing(List<string> appropriateValues, List<string> expectedValues)
        {
            List<bool> results = new List<bool>();
            for (int i = 0; i < appropriateValues.Count; i++)
            {
                string apVal = appropriateValues[i];
                string exVal = expectedValues[i];

                if (!exVal.Equals("") && !apVal.Equals("") && exVal.Equals(apVal))
                {
                    results.Add(true);
                }
                else
                {
                    results.Add(false);
                }
            }
            return results;
        }
    }
}
