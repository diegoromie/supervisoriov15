using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using NationalInstruments.DAQmx;
using NationalInstruments;

namespace SupervisorioV3
{
    public partial class Form1 : Form
    {
        private Task runningTask;
        private Task analogRead;
        private Task analogWrite;
        //private Task digitalRead;
        private Task digitalWrite;
        private AsyncCallback analogCallback;
        private AnalogMultiChannelReader analogInReader;
        //private DigitalSingleChannelReader digitalReader;
        private DigitalSingleChannelWriter digitalWriter;
        private AnalogWaveform<double>[] data;
        private DataColumn[] DataColumn = null;
        private DataTable dataTable = null;
        private AnalogSingleChannelWriter analogOutWriter;

        private double[] v1;
        private double[] v2;
        private double[] v3;
        private double[] i1;
        private double[] i2;
        private double[] i3;

        private float eixo_y = 0;
        private float eixo_1 = 0;
        private float eixo_2 = 0;
        private float eixo_3 = 0;
        private float eixo_4 = 0;
        private float eixo_5 = 0;
        private float eixo_6 = 0;
        private float eixo_7 = 0;
        private float eixo_8 = 0;
        private float eixo_9 = 0;
        private float somadequad_0 = 0;
        private float somadequad_1 = 0;
        private float somadequad_2 = 0;
        private float somadequad_3 = 0;
        private float somadequad_4 = 0;
        private float somadequad_5 = 0;
        private float somadequad_6 = 0;
        private float somadequad_7 = 0;
        private float somadequad_8 = 0;
        private float somadequadpot_0 = 0;
        private float somadequadpot_1 = 0;
        private float somadequadpot_2 = 0;
        private float somadevelocidade = 0;

        double v1rms = 0;
        double v2rms = 0;
        double v3rms = 0;
        double vrms11 = 0;
        double vrms12 = 0;
        double vrms13 = 0;
        double i1rms = 0;
        double i2rms = 0;
        double i3rms = 0;
        double p1rms = 0;
        double p2rms = 0;
        double p3rms = 0;
        double fp1 = 0;
        double fp2 = 0;
        double fp3 = 0;
        double velocidade;
        string v1txt;
        bool flag = false;
        bool flag1 = false;
        int K = 0;
        bool carga1Butao = false;
        bool carga2Butao = false;
        bool onOffButao = false;            //se falso = desligado
        bool hAhButao = false;              //se falso = horario
        int contador = 0;
        double auxvelocidade = 0;
        double tfaceleracao = 2;            //tempo de aceleracao
        double tfcarga = 8;                 //tempo carga permanece ligada + tempo aceleracao
        double tfdesaceleracao = 10;        //tempo aceleracao + tempo carga ligada + tempo desaceleracao


        public Form1()
        {
            InitializeComponent();
       
            dataTable = new DataTable();
            offButton.Enabled = false;
            stopButton.Enabled = false;
            startButton.Enabled = true;
            horarioButton.Enabled = false;
            ahorarioButton.Enabled = true;

            timer2.Interval = 100;        //100 ms

        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (runningTask == null)
                try
                {
                    stopButton.Enabled = true;
                    startButton.Enabled = false;

                    //cria novas tasks
                    analogRead = new Task();
                    analogWrite = new Task();
                    //digitalRead = new Task();
                    digitalWrite = new Task();

                    //cria canal de leitura de tensão
                    analogRead.AIChannels.CreateVoltageChannel("Dev1/ai0,Dev1/ai1,Dev1/ai2,Dev1/ai3,Dev1/ai4,Dev1/ai5,Dev1/ai6,Dev1/ai7,Dev1/ai8,Dev1/ai9",
                        "",
                        AITerminalConfiguration.Rse,
                        -10,
                        10,
                        AIVoltageUnits.Volts);

                    //cria canal de leitura digital
                    //digitalRead.DIChannels.CreateChannel("Dev1/port0/line0:7",
                    //    "",
                    //    ChannelLineGrouping.OneChannelForAllLines);

                    //digitalReader = new DigitalSingleChannelReader(digitalRead.Stream);
                    //loopTimer.Enabled = true;

                    //cria canal de escrita digital
                    digitalWrite.DOChannels.CreateChannel("Dev1/port1/line0:7",
                        "",
                        ChannelLineGrouping.OneChannelForAllLines);
                    digitalWriter = new DigitalSingleChannelWriter(digitalWrite.Stream);
                    loopTimer1.Enabled = true;

                    //configuração dos parametros de tempo
                    analogRead.Timing.ConfigureSampleClock("",
                        100000,
                        SampleClockActiveEdge.Rising,
                        SampleQuantityMode.ContinuousSamples,
                        100000);

                    //verifica as tasks
                    analogRead.Control(TaskAction.Verify);

                    //prepara a tabela de dados
                    InitializeDataTable(analogRead.AIChannels, ref dataTable);

                    runningTask = analogRead;         //task que esta sendo executada
                    analogInReader = new AnalogMultiChannelReader(analogRead.Stream);
                    analogCallback = new AsyncCallback(analogInCallback);

                    //synchronize callbacks
                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(Convert.ToInt32(100000),
                        analogCallback, analogRead);
                    timer1.Enabled = true;
                }
                catch (DaqException exception)
                {
                    //mostra erros
                    MessageBox.Show(exception.Message);
                    analogRead.Dispose();
                    analogWrite.Dispose();
                    //digitalRead.Dispose();
                    digitalWrite.Dispose();
                    stopButton.Enabled = false;
                    startButton.Enabled = true;
                    loopTimer.Enabled = false;
                    loopTimer1.Enabled = false;
                    timer1.Enabled = false;
                }
        }

        private void analogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    //le os dados nos canais
                    data = analogInReader.EndReadWaveform(ar);
                    dataToDataTable(data, ref dataTable);
                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(100000),
                        analogCallback, analogRead, data);
                    v1 = data[6].GetRawData(); //v1
                    v2 = data[2].GetRawData(); //v2
                    v3 = data[4].GetRawData(); //v3
                    i1 = data[1].GetRawData(); //i1
                    i2 = data[3].GetRawData(); //i2
                    i3 = data[5].GetRawData(); //i3 * calculada

                    //txt
                    StreamWriter valor = new StreamWriter("c:\\arquivos-txt\\arquivo.txt", true, Encoding.ASCII);
                    StreamWriter valor1 = new StreamWriter("c:\\arquivos-txt1\\arquivo.txt", true, Encoding.ASCII);

                    for (int i = 0; i < 100000; i++)
                    {
                        eixo_y = (Convert.ToSingle(v1[i]) * (220 / 6) - Convert.ToSingle(v3[i]) * (220 / 6)) * 2 / 3 * 17320 / 10000 * 219 / 223;  //v1-v3
                        eixo_1 = ((Convert.ToSingle((i1[i]) - Convert.ToDouble(Adji3NumericUpDown.Value))) * (1000 / 185)); //I1
                        eixo_2 = (Convert.ToSingle(v2[i]) * (220 / 6) - Convert.ToSingle(v1[i]) * (220 / 6)) * 2 / 3 * 17320 / 10000 * 219 / 223; //v2-v1
                        eixo_3 = ((Convert.ToSingle(((i2[i])) - Convert.ToDouble(Adji2NumericUpDown.Value))) * (1000 / 185)); //I2
                        eixo_4 = (Convert.ToSingle(v3[i]) * (220 / 6) - Convert.ToSingle(v2[i]) * (220 / 6)) * 2 / 3 * 17320 / 10000 * 219 / 223;
                        eixo_5 = ((-eixo_1 - eixo_3));      //I3 Calculada
                        eixo_6 = (Convert.ToSingle(i3[i]) * (1800000 / 10052)); //vel
                        eixo_7 = (Convert.ToSingle(i3[i])); //v1 //inutil
                        eixo_8 = (Convert.ToSingle(i3[i])); //v2 //inutil
                        eixo_9 = (Convert.ToSingle(i3[i])); //v3 //inutil

                        //variaveis para o txt
                        v1txt = Convert.ToString(eixo_y);
                        v1txt = v1txt + " " + Convert.ToString(eixo_1) + " " + Convert.ToString(eixo_2) + " " + Convert.ToString(eixo_3) + " " + Convert.ToString(eixo_4) + " " + Convert.ToString(eixo_5) + " " + Convert.ToString(eixo_6) + " " + Convert.ToString(eixo_7) + " " + Convert.ToString(eixo_8) + " " + Convert.ToString(eixo_9) + " " + Convert.ToString(auxvelocidade);


                        if (flag == true)
                        {
                            K++;
                            valor.WriteLine(v1txt);

                        }
                        if (K == (100000 * (Convert.ToDouble(numericUpDown1.Value))))
                        {
                            flag = false;
                            K = 0;
                            valor.Close();
                        }

                        if (flag1 == true)
                            valor1.WriteLine(v1txt);
                        if (flag1 == false)
                            valor1.Close();

                        somadequad_0 += (eixo_y * eixo_y);
                        somadequad_1 += eixo_1 * eixo_1;
                        somadequad_2 += eixo_2 * eixo_2;
                        somadequad_3 += eixo_3 * eixo_3;
                        somadequad_4 += eixo_4 * eixo_4;
                        somadequad_5 += eixo_5 * eixo_5;
                        somadequad_6 += eixo_7 * eixo_7;
                        somadequad_7 += eixo_8 * eixo_8;
                        somadequad_8 += eixo_9 * eixo_9;
                        somadequadpot_0 += (-eixo_y * eixo_1);
                        somadequadpot_1 += (-eixo_2 * eixo_3);
                        somadequadpot_2 += (-eixo_4 * eixo_5);
                        somadevelocidade += eixo_6;
                    }

                    valor.Close();
                    valor1.Close();

                    v1rms = Math.Sqrt(somadequad_0 / 100000);
                    i1rms = Math.Sqrt(somadequad_1 / 100000);
                    p1rms = somadequadpot_0 / 100000;

                    v2rms = Math.Sqrt(somadequad_2 / 100000);
                    i2rms = Math.Sqrt(somadequad_3 / 100000);
                    p2rms = (somadequadpot_1 / 100000);

                    v3rms = Math.Sqrt(somadequad_4 / 100000);
                    i3rms = Math.Sqrt(somadequad_5 / 100000);
                    p3rms = (somadequadpot_2 / 100000);

                    fp1 = (p1rms) / (i1rms * v1rms);
                    fp2 = (p2rms) / (i2rms * v2rms);
                    fp3 = (p3rms) / (i3rms * v3rms);

                    velocidade = somadevelocidade / 100000;
                    vrms11 = Math.Sqrt(somadequad_6 / 100000);
                    vrms12 = Math.Sqrt(somadequad_7 / 100000);
                    vrms13 = Math.Sqrt(somadequad_8 / 100000);

                    vabAuxTextBox.Text = vrms11.ToString("n3");
                    vbcAuxTextBox.Text = vrms11.ToString("n3");
                    vcaAuxTextBox.Text = vrms11.ToString("n3");

                    vrms1TextBox.Text = v1rms.ToString("n3");
                    vrms2TextBox.Text = v2rms.ToString("n3");
                    vrms3TextBox.Text = v3rms.ToString("n3");
                    irms1TextBox.Text = i1rms.ToString("n3");
                    irms2TextBox.Text = i2rms.ToString("n3");
                    irms3TextBox.Text = i3rms.ToString("n3");
                    p1TextBox.Text = p1rms.ToString("n3");
                    p2TextBox.Text = p2rms.ToString("n3");
                    p3TextBox.Text = p3rms.ToString("n3");
                    fp1TextBox.Text = fp1.ToString("n3");
                    fp2TextBox.Text = fp2.ToString("n3");
                    fp3TextBox.Text = fp3.ToString("n3");
                    fp3fTextBox.Text = fp1.ToString("n3");
                    pAtivaTextBox.Text = ((p1rms + p2rms + p3rms)).ToString("n3");
                    pAparenteTextBox.Text = ((i1rms * v1rms) + (i2rms * v2rms) + (i3rms * v3rms)).ToString("n3");
                    pReativaTextBox.Text = (((i1rms * v1rms) + (i2rms * v2rms) + (i3rms * v3rms)) * Math.Sin(Math.Acos(fp1))).ToString("n3");

                    somadequad_0 = 0;
                    somadequad_1 = 0;
                    somadequad_2 = 0;
                    somadequad_3 = 0;
                    somadequad_4 = 0;
                    somadequad_5 = 0;
                    somadequad_6 = 0;
                    somadequad_7 = 0;
                    somadequad_8 = 0;
                    somadequadpot_0 = 0;
                    somadequadpot_1 = 0;
                    somadequadpot_2 = 0;
                    somadevelocidade = 0;
                    //speedTextBox.Text = velocidade.ToString("n3");
                }
            }
            catch (DaqException exception)
            {
                //mostra erros
                MessageBox.Show(exception.Message);
                runningTask = null;
                analogRead.Dispose();
                analogWrite.Dispose();
                //digitalRead.Dispose();
                digitalWrite.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
                loopTimer.Enabled = false;
                loopTimer1.Enabled = false;
                timer1.Enabled = false;
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
                //dispose of the task
                runningTask = null;
                analogRead.Dispose();
                analogWrite.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
                //bits omitidos
                dataTable.Clear();
                dataTable.Rows.Clear();
                dataTable.Columns.Clear();
                loopTimer.Enabled = false;
                loopTimer1.Enabled = false;
                timer1.Enabled = false;
                bit0LpictureBox.Image = Properties.Resources.ledVerdeOff;
                bit1LpictureBox.Image = Properties.Resources.ledVerdeOff;
                bit2LpictureBox.Image = Properties.Resources.ledVerdeOff;
                bit3LpictureBox.Image = Properties.Resources.ledVerdeOff;
        }

        private void dataToDataTable(AnalogWaveform<double>[] sourceArray, ref DataTable dataTable)
        {
            //iterar sobre os canais
            int currentLineIndex = 0;
            foreach (AnalogWaveform<double> waveform in sourceArray)
            {
                for (int sample = 0; sample < waveform.Samples.Count; ++sample)
                {
                    if (sample == 10)
                        break;

                    dataTable.Rows[sample][currentLineIndex] = waveform.Samples[sample].Value;
                }
                currentLineIndex++;
            }
        }

        public void InitializeDataTable(AIChannelCollection channelCollection, ref DataTable data)
        {
            int numOfChannels = channelCollection.Count;
            data.Rows.Clear();
            DataColumn = new DataColumn[numOfChannels];
            int numOfRows = 10;

            for (int currentChannelIndex = 0; currentChannelIndex < numOfChannels; currentChannelIndex++)
            {
                DataColumn[currentChannelIndex] = new DataColumn();
                DataColumn[currentChannelIndex].DataType = typeof(double);
                DataColumn[currentChannelIndex].ColumnName = channelCollection[currentChannelIndex].PhysicalName;
            }
            data.Columns.AddRange(DataColumn);

            for (int currentDataIndex = 0; currentDataIndex < numOfRows; currentDataIndex++)
            {
                object[] rowArr = new object[numOfChannels];
                data.Rows.Add(rowArr);
            }
        }

        private void loopTimer_Tick(object sender, EventArgs e)
        {
        //    try
        //    {
        //        bool[] readData;
        //
        //        //le o canal digital
        //        readData = digitalReader.ReadSingleSampleMultiLine();
        //        //muda as imagens
        //        if (readData[0] == true)
        //            bit0LpictureBox.Image = Properties.Resources.LedVerdeOn;
        //        else
        //            bit0LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //        if (readData[1] == true)
        //            bit1LpictureBox.Image = Properties.Resources.LedVerdeOn;
        //        else
        //            bit1LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //        if (readData[2] == true)
        //            bit2LpictureBox.Image = Properties.Resources.LedVerdeOn;
        //        else
        //            bit2LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //        if (readData[3] == true)
        //            bit3LpictureBox.Image = Properties.Resources.LedVerdeOn;
        //        else
        //            bit3LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //    }
        //    catch
        //    {
        //        loopTimer.Enabled = false;
        //        loopTimer1.Enabled = false;
        //        timer1.Enabled = false;
        //        digitalRead.Dispose();
        //        digitalWrite.Dispose();
        //        analogRead.Dispose();
        //        analogWrite.Dispose();
        //        bit0LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //        bit1LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //        bit2LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //        bit3LpictureBox.Image = Properties.Resources.ledVerdeOff;
        //    }
        }

        private void loopTimer1_Tick(object sender, EventArgs e)
        {
            textBox1.Text = Convert.ToString(flag);
            textBox2.Text = Convert.ToString(auxvelocidade);
            try
            {
                bool[] writeData = new bool[8];
                //carga1Butao = writeData[0];
                //carga2Butao = writeData[1];
                //onOffButao = writeData[2];
                //hAhButao = writeData[4];

                //writeData[0] = carga1Butao;
                //writeData[1] = carga2Butao;
                //writeData[2] = onOffButao;
                //writeData[3] = hAhButao;
                writeData[0] = onOffButao;
                writeData[1] = hAhButao;
                writeData[4] = carga1Butao;
                writeData[5] = carga2Butao;


                //write the digital channel
                //bits omitidos
                digitalWriter.WriteSingleSampleMultiLine(true, writeData);

                //muda as imagens dos leds
                if (writeData[0] == true)
                    bit0EpictureBox.Image = Properties.Resources.LedVerdeOn;
                else
                    bit0EpictureBox.Image = Properties.Resources.ledVerdeOff;
                if (writeData[1] == true)
                    bit1EpictureBox.Image = Properties.Resources.LedVerdeOn;
                else
                    bit1EpictureBox.Image = Properties.Resources.ledVerdeOff;
                if (writeData[2] == true)
                    bit2EpictureBox.Image = Properties.Resources.LedVerdeOn;
                else
                    bit2EpictureBox.Image = Properties.Resources.ledVerdeOff;
                if (writeData[3] == true)
                    bit3EpictureBox.Image = Properties.Resources.LedVerdeOn;
                else
                    bit3EpictureBox.Image = Properties.Resources.ledVerdeOff;

                //imagem do sentido de giro do motor
                //if (onOffButao == true)
                //{
                //    if (hAhButao == false)      //motor gira no sentido horario
                //    {
                //        //troca as imagens do imsentgiro no sentid horario
                //    }
                //    else
                //    {
                //        //troca as imagens do imsentgiro no sentido anti horario
                //        imSentGiro.Image = Properties.Resources.sentAH1;
                //        imSentGiro.Image = Properties.Resources.sentAH2;
                //        imSentGiro.Image = Properties.Resources.sentAH3;
                //        imSentGiro.Image = Properties.Resources.sentAH4;
                //        imSentGiro.Image = Properties.Resources.sentAH5;
                //        imSentGiro.Image = Properties.Resources.sentAH6;
                //        imSentGiro.Image = Properties.Resources.sentAH7;
                //        imSentGiro.Image = Properties.Resources.sentAH8;
                //    }
                //}
            }
            catch (DaqException ex)
            {
                MessageBox.Show(ex.Message);
                loopTimer.Enabled = false;
                loopTimer1.Enabled = false;
                timer1.Enabled = false;
                //digitalRead.Dispose();
                digitalWrite.Dispose();
                analogRead.Dispose();
                analogWrite.Dispose();
                startButton.Enabled = true;
                stopButton.Enabled = false;
                bit0EpictureBox.Image = Properties.Resources.ledVerdeOff;
                bit1EpictureBox.Image = Properties.Resources.ledVerdeOff;
                bit2EpictureBox.Image = Properties.Resources.ledVerdeOff;
                bit3EpictureBox.Image = Properties.Resources.ledVerdeOff;
            }
        }

        private void startTxtButton_Click(object sender, EventArgs e)
        {
            flag = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            analogWrite = new Task();

            //cria canal escrita tensao
            analogWrite.AOChannels.CreateVoltageChannel("Dev1/ao0", "", 0, 10, AOVoltageUnits.Volts);
            analogOutWriter = new AnalogSingleChannelWriter(analogWrite.Stream);
            //analogOutWriter.WriteSingleSample(true, Convert.ToDouble(speedControlUpDown.Value));
            analogOutWriter.WriteSingleSample(true, auxvelocidade);
        }

        private void Adji1NumericUpDown_ValueChanged(object sender, EventArgs e)
        {

        }

        private void Adji2NumericUpDown_ValueChanged(object sender, EventArgs e)
        {

        }

        private void Adji3NumericUpDown_ValueChanged(object sender, EventArgs e)
        {

        }

        private void carga1Button_Click(object sender, EventArgs e)
        {
            if (carga1Butao == true)
            {
                carga1Butao = false;
                carga1Button.Image = Properties.Resources.CargaOFF;
            }
            else
            {
                carga1Butao = true;
                carga1Button.Image = Properties.Resources.CargaON;
            }
        }

        private void onButton_Click(object sender, EventArgs e)
        {
            onButton.Enabled = false;
            offButton.Enabled = true;
            onOffButao = true;          //onOffButao = true ===> liga o motor
        }

        private void offButton_Click(object sender, EventArgs e)
        {
            offButton.Enabled = false;
            onButton.Enabled = true;
            onOffButao = false;         //onOffButao = false ===> desliga o motor
            timer2.Enabled = false;
        }

        private void horarioButton_Click(object sender, EventArgs e)
        {
            //antihorarioButao = false;
            //horarioButao = true;
            horarioButton.Enabled = false;
            ahorarioButton.Enabled = true;
            hAhButao = false;           //hAhButao = false ===> sentido horario
        }

        private void ahorarioButton_Click(object sender, EventArgs e)
        {
            //horarioButao = false;
            //antihorarioButao = true;
            horarioButton.Enabled = true;
            ahorarioButton.Enabled = false;
            hAhButao = true;            //hAhButao = true ===> sentido anti horario
        }

        private void carga2Button_Click(object sender, EventArgs e)
        {
            if (carga2Butao == true)
            {
                carga2Butao = false;
                carga2Button.Image = Properties.Resources.CargaOFF;
            }
            else
            {
                carga2Butao = true;
                carga2Button.Image = Properties.Resources.CargaON;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (K <= tfaceleracao*100000)
            {
                label37.Text = "Acelerando";
                onOffButao = true;
                onButton.Enabled = false;
                offButton.Enabled = true;
                //auxvelocidade = K / 100000;
                auxvelocidade = (10 * K) / (tfaceleracao * 100000);
            }
            else if (K > tfaceleracao*100000 && K <=tfcarga*100000)
            {
                //contador = contador + 1;
                //label37.Text = contador.ToString();
                label37.Text = "Ensaio Carga";
                carga1Butao = true;
                carga1Button.Image = Properties.Resources.CargaON;
                auxvelocidade = 10;
            }
            else if (K > tfcarga*100000 && K < tfdesaceleracao*100000)
            {
                label37.Text = "Desacelerando";
                carga1Butao = false;
                carga1Button.Image = Properties.Resources.CargaOFF;
                //auxvelocidade = -K / 100000 + 30;
                auxvelocidade = (10 * K) / (100000 * (tfcarga - tfdesaceleracao)) - (10 * tfdesaceleracao) / (tfcarga - tfdesaceleracao);
            }
            else if (K == tfdesaceleracao*100000)
            {
                label37.Text = "Fim do Ensaio";
                //contador = 0;
                timer2.Enabled = false;
                onButton.Enabled = true;
                onOffButao = false;
                offButton.Enabled = false;
                flag = false;
                auxvelocidade = 0;
            }
            contador = K/100000;
            label38.Text = contador.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            contador = 0;
            timer2.Enabled = true;
            flag = true;            // inicia a aquisicao

            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
