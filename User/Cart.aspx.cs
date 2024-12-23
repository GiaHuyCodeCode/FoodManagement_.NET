﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FoodShop.User
{
    public partial class Cart : System.Web.UI.Page
    {
        SqlConnection con;
        SqlCommand cmd;
        SqlDataAdapter sda;
        DataTable dt;
        decimal grandToal = 0;

        protected void Page_Load(object sender, EventArgs e)
        {
            if(!IsPostBack)
            {
                if(Session["userId"] == null)
                {
                    Response.Redirect("Login.aspx");
                }
                else
                {
                    getCartItems();
                }
            }
        }

        void getCartItems()
        {
            con = new SqlConnection(Connection.GetConnectionString());
            cmd = new SqlCommand("Cart_Crud", con);
            cmd.Parameters.AddWithValue("@Action", "SELECT");
            cmd.Parameters.AddWithValue("@UserId", Session["userID"]);
            cmd.CommandType = CommandType.StoredProcedure;
            sda = new SqlDataAdapter(cmd);
            dt = new DataTable();
            sda.Fill(dt);
            rCartItem.DataSource = dt;
            if (dt.Rows.Count == 0)
            {
                rCartItem.FooterTemplate = null;
                rCartItem.FooterTemplate = new CustomTemplate(ListItemType.Footer);
            }
            rCartItem.DataBind();
        }

        protected void rCartItem_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            Utils utils = new Utils();
            if (e.CommandName == "remove")
            {
                con = new SqlConnection(Connection.GetConnectionString());
                cmd = new SqlCommand("Cart_Crud", con);
                cmd.Parameters.AddWithValue("@Action", "DELETE");
                cmd.Parameters.AddWithValue("@ProductId", e.CommandArgument);
                cmd.Parameters.AddWithValue("@UserId", Session["userID"]);
                cmd.CommandType = CommandType.StoredProcedure;
                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    getCartItems();
                    //Cart count
                    Session["cartCount"] = utils.cartCount(Convert.ToInt32(Session["userID"]));
                }
                catch (Exception ex)
                {
                    Response.Write("<script>alert('" + ex.Message + "')</script>");
                }
                finally
                {
                    con.Close();
                }
            }
            else if (e.CommandName == "updateCart")
            {
                bool isCartUpdated = false;
                grandToal = 0; // Reset grandTotal before recalculation
                for (int i = 0; i < rCartItem.Items.Count; i++)
                {
                    if (rCartItem.Items[i].ItemType == ListItemType.Item || rCartItem.Items[i].ItemType == ListItemType.AlternatingItem)
                    {
                        TextBox quantity = rCartItem.Items[i].FindControl("txtQuantity") as TextBox;
                        HiddenField _productId = rCartItem.Items[i].FindControl("hdnProductId") as HiddenField;
                        HiddenField _quantity = rCartItem.Items[i].FindControl("hdnQuantity") as HiddenField;
                        Label totalPrice = rCartItem.Items[i].FindControl("lblTotalPrice") as Label;
                        Label productPrice = rCartItem.Items[i].FindControl("lblPrice") as Label;

                        int quantityFromCart = Convert.ToInt32(quantity.Text);
                        int ProductId = Convert.ToInt32(_productId.Value);
                        int quantityFromDB = Convert.ToInt32(_quantity.Value);
                        bool isTrue = false;
                        int updatedQuantity = 1;

                        if (quantityFromCart > quantityFromDB)
                        {
                            updatedQuantity = quantityFromCart;
                            isTrue = true;
                        }
                        else if (quantityFromDB < quantityFromCart)
                        {
                            updatedQuantity = quantityFromCart;
                            isTrue = true;
                        }

                        if (isTrue)
                        {
                            isCartUpdated = utils.updateCartQuantity(updatedQuantity, ProductId, Convert.ToInt32(Session["userID"]));
                        }

                        // Recalculate total price for the item
                        decimal calTotalPrice = Convert.ToDecimal(productPrice.Text) * quantityFromCart;
                        totalPrice.Text = calTotalPrice.ToString();
                        grandToal += calTotalPrice;
                    }
                }

                // Update session for grand total
                Session["grandTotal"] = grandToal;

                getCartItems(); // Refresh the cart items
            }
            else if (e.CommandName == "checkout")
            {
                bool isTrue = false;
                string pName = string.Empty;
                //Check item quantity
                for (int i = 0; i < rCartItem.Items.Count; i++)
                {
                    if (rCartItem.Items[i].ItemType == ListItemType.Item || rCartItem.Items[i].ItemType == ListItemType.AlternatingItem)
                    {
                        HiddenField _productId = rCartItem.Items[i].FindControl("hdnProductId") as HiddenField;
                        HiddenField _cartQuantity = rCartItem.Items[i].FindControl("hdnQuantity") as HiddenField;
                        HiddenField _productQuantity = rCartItem.Items[i].FindControl("hdnPrdQuantity") as HiddenField;
                        Label productName = rCartItem.Items[i].FindControl("lblProductName") as Label;
                        int productId = Convert.ToInt32(_productId.Value);
                        int cartQuantity = Convert.ToInt32(_cartQuantity.Value);
                        int productQuantity = Convert.ToInt32(_productQuantity.Value);
                        if (productQuantity > cartQuantity && productQuantity > 2)
                        {
                            isTrue = true;
                        }
                        else
                        {
                            isTrue = false;
                            pName = productName.Text.ToString();
                            break;
                        }
                    }
                }
                if (isTrue)
                {
                    Response.Redirect("Payment.aspx");
                }
                else
                {
                    lblMsg.Visible = true;
                    lblMsg.Text = pName + " is out of stock!";
                    lblMsg.CssClass = "alert alert-warning";
                }
            }
        }


        protected void rCartItem_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                Label totalPrice = e.Item.FindControl("lblTotalPrice") as Label;
                Label productPrice = e.Item.FindControl("lblPrice") as Label;
                TextBox quantity = e.Item.FindControl("txtQuantity") as TextBox;
                decimal calTotalPrice = Convert.ToDecimal(productPrice.Text) * Convert.ToInt32(quantity.Text);
                totalPrice.Text = calTotalPrice.ToString();
                grandToal += calTotalPrice;
            }
            Session["grandTotal"] = grandToal;
        }

        private sealed class CustomTemplate: ITemplate
        {
            private ListItemType ListItemType { get; set; }
            public CustomTemplate(ListItemType type)
            {
                ListItemType = type;
            }
            public void InstantiateIn(Control container)
            {
                if(ListItemType == ListItemType.Footer)
                {
                    var footer = new LiteralControl("<tr><td colspan='5'><b>Your Cart is empty</b><a href='Menu.aspx' class='badge badge-info ml-2'>Continue Shopping </a></td></tr></tbody></table>");
                    container.Controls.Add(footer);
                }
            }
        }   
    }
}