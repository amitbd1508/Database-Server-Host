using Alchemy;
using Alchemy.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Alchemy.Handlers.WebSocket;
using Newtonsoft.Json;
using System.Web.Script.Serialization;

namespace DatabseServerHost
{
    public partial class MainForm : Form
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();

        System.Data.SQLite.SQLiteConnection sqlconn;
        private SQLiteDataAdapter DB;
        private DataSet DS = new DataSet();
        private DataTable DT = new DataTable();
        static string conn;

        public MainForm()
        {
            InitializeComponent();
            DatabaseInit();
            //richTextBox1.AppendText(GetAllStudent());
        }
        private string GetAllStudent(string userid)
        {
            string ct = "select * from Student where UserId="+userid;
            DB = new SQLiteDataAdapter(ct, sqlconn);
            DS.Reset();
            DB.Fill(DS);
           
            DT = DS.Tables[0];
            List<Student> students=new List<Student>();
            foreach (DataRow row in DT.Rows)
            {
               
                Student std = new Student();
                std.id = row["StudentId"].ToString();
                std.name = row["Name"].ToString();
                std.semester = row["Semister"].ToString();
                std.department = row["Department"].ToString();

                students.Add(std);

            }
                
            var json= JsonConvert.SerializeObject(students);
            return "A"+json;

        }

        private void DatabaseInit()
        {
            conn = @"Data Source =university.db; Version=3;New=False;Compress=True;";
            sqlconn = new SQLiteConnection(conn);
            try
            {
                sqlconn.Open();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            startServer();

        }
        void startServer()
        {

           try
            {
                var aServer = new WebSocketServer(8182, IPAddress.Any)
                {


                    OnReceive = OnReceive,
                    OnSend = OnSend,
                    OnConnect = OnConnect,
                    OnConnected = OnConnected,
                    OnDisconnect = OnDisconnect,
                    TimeOut = new TimeSpan(0, 5, 0)

                };
                richTextBox1.AppendText("Initializing......\n");
                aServer.Start();

                richTextBox1.AppendText("\nStart at " + aServer.Origin + ":8182" + "");
            }
            catch(Exception ex)
            {
                richTextBox1.AppendText("Cannot not start ");
            }
        }
        private void OnConnected(UserContext context)
        {

            Invoke(new Action(() => richTextBox1.AppendText("\nClient Connection From : " + context.ClientAddress.ToString())));
            

        }

        private void OnDisconnect(UserContext context)
        {

            Invoke(new Action(() => richTextBox1.AppendText(context.ClientAddress + "\nDisconnected............." + "\n")));

        }

        private void OnConnect(UserContext context)
        {

            Invoke(new Action(() => richTextBox1.AppendText(context.ClientAddress + "\nConnecting............." + "")));
        }

        private void OnSend(UserContext context)
        {


        }

        private void OnReceive(UserContext context)
        {



            try
            {
                Invoke(new Action(() => richTextBox1.AppendText("\n" + context.DataFrame)));

                if (context.DataFrame.ToString().Contains("Active?"))
                    context.Send("Active?");

                else if (context.DataFrame.ToString()[0] == 'G')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    Student std = serializer.Deserialize<Student>(mess);
                    context.Send(GetAllStudent(std.userid));
                }

                else if (context.DataFrame.ToString()[0] == 'I')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    Student std = serializer.Deserialize<Student>(mess);
                    context.Send(insert(std));



                }
                else if (context.DataFrame.ToString()[0] == 'U')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    Student std = serializer.Deserialize<Student>(mess);
                    context.Send(Update(std));
                }

                else if (context.DataFrame.ToString()[0] == 'S')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    Student std = serializer.Deserialize<Student>(mess);
                    context.Send(Select(std.id,std.userid));
                }
                else if (context.DataFrame.ToString()[0] == 'D')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    Student std = serializer.Deserialize<Student>(mess);
                   
                    context.Send(Delete(std.id,std.userid));
                }





                //=================================
                else if (context.DataFrame.ToString()[0] == 'P')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    User user = serializer.Deserialize<User>(mess);

                    context.Send(UpdateUser(user.id, user.password));
                }
                else if (context.DataFrame.ToString()[0] == 'L')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    User user = serializer.Deserialize<User>(mess);

                    context.Send(Auth(user));
                }
                else if (context.DataFrame.ToString()[0] == 'R')
                {
                    string mess = context.DataFrame.ToString().Remove(0, 1);
                    User user = serializer.Deserialize<User>(mess);

                    context.Send(Registation(user));
                    
                }


            }
            catch(Exception ex)
            {
                MessageBox.Show("Convertion Exception");
            }

        }

        private string UpdateUser(string id, string password)
        {
            try
            {
                using (var cmd = new SQLiteCommand("UPDATE  User SET Password=@Password  Where Id=@Id", sqlconn))
                {

                    cmd.Parameters.Add("@Id", DbType.Int32).Value = Convert.ToInt32(id);
                    cmd.Parameters.Add("@Password", DbType.String).Value = password;
                   


                    cmd.ExecuteNonQuery();


                    return "User Updated";
                }
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText("Update Failed");
                return "User Not Updated";
            }
        }

        private string  Registation(User user)
        {
            try
            {
                using (var cmd = new SQLiteCommand("INSERT INTO User (Username, Password) values(@Username,@Password)", sqlconn))
                {

                    cmd.Parameters.Add("@Username", DbType.String).Value = user.username;
                    cmd.Parameters.Add("@Password", DbType.String).Value = user.password;
                    

                    cmd.ExecuteNonQuery();


                    return "User Registation Sucessfull";
                }
            }
            catch (Exception ec)
            {
                richTextBox1.AppendText("Insert Failed");
                return "User Registation Unsucessfull";
            }
        }

        private string Auth(User user)
        {
            try
            {
                string ct = "select Id from User Where Username= '" + user.username+"' and Password = '"+user.password+"'";
                DB = new SQLiteDataAdapter(ct, sqlconn);
                DS.Reset();
                DB.Fill(DS);

                DT = DS.Tables[0];
              
                Student std = new Student();

                DataRow row = DT.Rows[0];
                user.id = row["Id"].ToString();
                
                

                
                return user.id;
            }
            catch (Exception ex)
            {
                Invoke(new Action(() => richTextBox1.AppendText("No Such a ID")));
                return "User Auth failed ";

            }
        }

        private string Select(string mess,string userid)
        {
            try
            {
                string ct = "select * from Student Where StudentId= '" + mess+"' and UserId= '"+userid+"'";
                DB = new SQLiteDataAdapter(ct, sqlconn);
                DS.Reset();
                DB.Fill(DS);

                DT = DS.Tables[0];
                List<Student> students = new List<Student>();
                Student std = new Student();

                DataRow row = DT.Rows[0];
                std.id = row["StudentId"].ToString();
                std.name = row["Name"].ToString();
                std.semester = row["Semister"].ToString();
                std.department = row["Department"].ToString();
                std.userid = row["UserId"].ToString();
                students.Add(std);

                var json = JsonConvert.SerializeObject(students);
                return "S" + json;
            }
            catch(Exception ex)
            {
                Invoke(new Action(() => richTextBox1.AppendText("No Such a ID")));
                return "replay Select false";

            }
        }

        private string Update(Student std)
        {
            try
            {
                using (var cmd = new SQLiteCommand("UPDATE  Student SET StudentID=@StudentID ,UserId=@UserId, Name=@Name , Department=@Department , Semister=@Semister Where StudentID=@StudentID", sqlconn))
                {

                    cmd.Parameters.Add("@StudentID", DbType.String).Value = std.id;
                    cmd.Parameters.Add("@Name", DbType.String).Value = std.name;
                    cmd.Parameters.Add("@Department", DbType.String).Value = std.department;
                    cmd.Parameters.Add("@Semister", DbType.String).Value = std.semester;
                    cmd.Parameters.Add("@UserId", DbType.String).Value = std.userid;

                    cmd.ExecuteNonQuery();


                    return "replay Update true";
                }
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText("Update Failed");
                return "replay Update false";
            }
        }
        private string Delete(string std,string userid)
        {
            try
            {
                using (var cmd = new SQLiteCommand("Delete from Student Where StudentID=@StudentID and UserId=@UserId", sqlconn))
                {

                    cmd.Parameters.Add("@StudentID", DbType.String).Value = std;
                    cmd.Parameters.Add("@UserId", DbType.String).Value = userid;
                    cmd.ExecuteNonQuery();
                    return "replay Delete true";
                }
            }
            catch (Exception ec)
            {
                richTextBox1.AppendText("Delete Failed");
                return "replay Delete false";
            }
        }
        string insert(Student std)
        {
            //sql = @"insert into products (name, description,price,type,image) values ('" + txtName.Text + "', '" + rtxt.Text + TxtPrice.Text + "', '" + cmbType.SelectedItem + "');";
            // sql = @"insert into products (name, description,price,type,image) values ('" + txtName.Text + "', '" + rtxt.Text + "', '" + TxtPrice.Text + "', '" + cmbType.SelectedItem + "', '" + bitmapToString(new Bitmap(pictureBox1.Image)) + "');";
            //MessageBox.Show(sql);

            try
            {
                using (var cmd = new SQLiteCommand("INSERT INTO Student (StudentID, Name,Department,Semister,UserId) values(@StudentID,@Name,@Department,@Semister,@UserId)", sqlconn))
                {

                    cmd.Parameters.Add("@StudentID", DbType.String).Value = std.id;
                    cmd.Parameters.Add("@Name", DbType.String).Value = std.name;
                    cmd.Parameters.Add("@Department", DbType.String).Value = std.department;
                    cmd.Parameters.Add("@Semister", DbType.String).Value = std.semester;
                    cmd.Parameters.Add("@UserId", DbType.String).Value = std.userid;
                    cmd.ExecuteNonQuery();
                    
                    
                    return "replay Create true";
                }
            }
            catch (Exception ec)
            {
                richTextBox1.AppendText("Insert Failed");
                return "replay Create false";
            }







        }



    }
}
