﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Visio = Microsoft.Office.Interop.Visio;

namespace OOSD_CASE_Tool
{
    public partial class SM_Obj_Attribute_Form : Form
    {
        /// <summary>
        /// Reference to the Shape that owns (called) this form and whose shape
        /// data is defined using this form.
        /// </summary>
        private Visio.Shape ownerShape;

        public SM_Obj_Attribute_Form(Visio.Shape Shape)
        {
            InitializeComponent();
            ownerShape = Shape;

            // Shape Data section stores all attributes for the Shape
            // as defined by the user through this form.
            Utilities.insertShapeDataSection(ownerShape);
        }

        private void SM_Obj_Attribute_Form_Load(object sender, EventArgs e)
        {
            // TODO: Loads all of the Shape's list of objects from its Shapesheet Data Section.
            objNameListBox.Items.AddRange(Utilities.getAllShapeNames(ownerShape).ToArray<string>());

            // Loads all of the Shape's list of operations from its Shapesheet Data Section.
            loadOperationNameList();
            // Sets the first operation in the list, if there is any, as the selected item
            // in the ListBox and displays its properties in the Operation Properties.
            if (operationNameListBox.Items.Count > 0)
            {
                operationNameListBox.SetSelected(0, true);
                string opName = operationNameListBox.SelectedItem.ToString();
                displayOperationProperties(opName);
            }
        }



        /// <summary>
        /// Displays the operation's properties in the operationPropertiesGroupBox.
        /// </summary>
        /// <param name="operationName">Name of operation to display.</param>
        private void displayOperationProperties(string operationName)
        {
            operationNameTextBox.Text = operationName;

            string rowName = "op_" + operationName + "_";

            nextStateTextBox.Text = Utilities.getDataSectionValueCell(ownerShape, rowName + "state");
            eventTextBox.Text = Utilities.getDataSectionValueCell(ownerShape, rowName + "event");
            controlTextBox.Text = Utilities.getDataSectionValueCell(ownerShape, rowName + "control");
        }

        /// <summary>
        /// Retrieves the list of operations (if any) associated with this Shape
        /// from the Data Section and loads the operationNameListBox with the names.
        /// </summary>
        private void loadOperationNameList()
        {
            HashSet<string> operationSet = new HashSet<string>();

            // All operation rows are stored in the form: 
            // op_[operation_name]_[operation_property] in the Label Cell
            short numRows = ownerShape.get_RowCount(CaseTypes.SHAPE_DATA_SECTION);
            for (short r = 0; r < numRows; ++r)
            {
                Visio.Cell labelCell = ownerShape.get_CellsSRC(CaseTypes.SHAPE_DATA_SECTION,
                    r, CaseTypes.DS_LABEL_CELL);

                string labelCellValue = labelCell.get_ResultStr(Visio.VisUnitCodes.visUnitsString);

                // we are only interested in operation-related rows
                if (labelCellValue.StartsWith("op_"))
                {
                    // we are only interested in the operation name
                    int startIndex = labelCellValue.IndexOf('_') + 1;
                    int endIndex = labelCellValue.LastIndexOf('_');
                    int opNameLen = endIndex - startIndex;
                    string opName = labelCellValue.Substring(startIndex, opNameLen);

                    operationSet.Add(Utilities.underscoreToSpace(opName));
                }
            }

            operationNameListBox.Items.AddRange(operationSet.ToArray());
        }

        /// <summary>
        /// Closes the form. Does not save any un-applied changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void operationNameTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Saves any new/updated form data to the Shapesheet Data Section.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void applyBtn_Click(object sender, EventArgs e)
        {
            saveOperation();
        }

        /// <summary>
        /// Saves an operation and its properties, taken from the operationPropertiesGroupBox
        /// input text boxes, in the Shapesheet Data Section.
        /// </summary>
        private void saveOperation()
        {
            // Shape Data section format
            //    row name                 ::  Value cell
            // op_[operation name]_        :: [operation name]
            // op_[operation name]_state   :: [state name]
            // op_[operation name]_event   :: [event]
            // op_[operation name]_control :: [control]

            string opName = operationNameTextBox.Text.Trim();
            // Must have an operation name
            if (opName == "")
            {
                MessageBox.Show("Must enter an Operation Name.");
            }
            else
            {
                string rowName = "op_" + opName + "_";

                Utilities.setDataSectionValueCell(ownerShape, rowName, opName);

                string stateName = nextStateTextBox.Text;
                Utilities.setDataSectionValueCell(ownerShape, rowName + "state", stateName);

                string eventName = eventTextBox.Text;
                Utilities.setDataSectionValueCell(ownerShape, rowName + "event", eventName);

                string controlName = controlTextBox.Text;
                Utilities.setDataSectionValueCell(ownerShape, rowName + "control", controlName);

                updateOperationsList(operationNameTextBox.Text);
            }
        }

        /// <summary>
        /// If the given operationName doesn't exist, add it to the operationNameListBox
        /// and set the newly added item as the SelectedItem in the ListBox.
        /// </summary>
        /// <param name="operationName">
        /// Name of the operation to add to the operationNameListBox.
        /// </param>
        private void updateOperationsList(string operationName)
        {
            ListBox.ObjectCollection opNames = operationNameListBox.Items;

            // only add the operation name if it doesn't already exist
            int itemIndex = Utilities.itemExists(opNames, operationName);
            if (itemIndex < 0)
            {
                opNames.Add(operationName);
                itemIndex = opNames.Count - 1;
            }

            operationNameListBox.SetSelected(itemIndex, true);
        }

        /// <summary>
        /// Clears out the input text boxes in the Operation Properties Group Box 
        /// to let user enter information for the new operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newOperationBtn_Click(object sender, EventArgs e)
        {
            Utilities.clearTextBoxInGroupBox(operationPropertiesGroupBox);
            operationNameListBox.ClearSelected();
        }

        /// <summary>
        /// Removes the currently selected operation name entry from the
        /// operationNameListBox and from the Shapesheet Data Section.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteOperationBtn_Click(object sender, EventArgs e)
        {
            Object selectedItem = operationNameListBox.SelectedItem;
            if (selectedItem != null)
            {
                string selectedValue = selectedItem.ToString();

                // Removes the item from the ListBox
                operationNameListBox.Items.Remove(selectedItem);

                // Removes the operation and its properties from the Shapesheet
                // All operation rows are prefixed with 'op_' in its name
                Utilities.deleteDataSectionRow(ownerShape, "op_" + selectedValue);
                Utilities.clearTextBoxInGroupBox(operationPropertiesGroupBox);
            } 
            else
            {
                MessageBox.Show("Select an Operation to delete.");
            }
        }

        /// <summary>
        /// When user selects an item in the operationNameListBox, display the
        /// operation's properties in the Operation Properties GroupBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void operationNameListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Object item = operationNameListBox.SelectedItem;

            if (item != null)
            {
                string opName = operationNameListBox.SelectedItem.ToString();
                displayOperationProperties(opName);
            }
        }

        private void objNameListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }
}