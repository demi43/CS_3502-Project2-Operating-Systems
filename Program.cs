using System;
using System.Collections.Generic;
using System.Linq;

namespace OPERATING_SYSTEM_PROJECT_2
{
    // --- Process Class for MLFQ ---
    public class MLFQProcess
    {
        public int ID { get; set; }
        public double ArrivalTime { get; set; }
        public double BurstTime { get; set; } // Original total burst time
        public double RemainingBurstTime { get; set; }
        public double StartTime { get; set; } = -1; // -1 indicates not started yet
        public double CompletionTime { get; set; }
        public double WaitingTime { get; set; }
        public double TurnaroundTime { get; set; }
        public double ResponseTime { get; set; }

        public int CurrentQueueLevel { get; set; }
        public double TimeInCurrentQuantum { get; set; } // Time used since last (re)entry into current queue/quantum

        public bool HasStarted { get { return StartTime != -1; } }

        public MLFQProcess(int id, double arrival, double burst)
        {
            ID = id;
            ArrivalTime = arrival;
            BurstTime = burst;
            RemainingBurstTime = burst;
            CurrentQueueLevel = 0; // Start in the highest priority queue
            TimeInCurrentQuantum = 0;
        }
    }

    internal class Program
    {
        // --- MLFQ Configuration Constants ---
        const int NUM_QUEUES = 3;
        // Time slices for RR queues (Q0, Q1). Q2 is FCFS (effectively infinite slice)
        static readonly double[] TIME_SLICES = { 8, 16, double.MaxValue }; // Ensure this line is inside the class
        const double BOOST_INTERVAL = 100; // Boost all processes to Q0 every 100 time units

        static void Main(string[] args)
        {
            string choice;
            do
            {
                // Display Menu (FCFS, SRTF, MLFQ, Exit)
                Console.WriteLine("\n--- CPU Scheduling Algorithm Simulator ---");
                Console.WriteLine("Select an algorithm to run:");
                Console.WriteLine("  1. First-Come, First-Served (FCFS)");
                Console.WriteLine("  2. Shortest Remaining Time First (SRTF)");
                Console.WriteLine("  3. Multi-Level Feedback Queue (MLFQ)");
                Console.WriteLine("  0. Exit");
                Console.Write("Enter your choice: ");

                choice = Console.ReadLine();
                int np = 0;

                // Get Number of Processes only if a valid algorithm is chosen
                if (choice == "1" || choice == "2" || choice == "3")
                {
                    bool validNp = false;
                    while (!validNp)
                    {
                        Console.Write("Enter number of processes: ");
                        string npInput = Console.ReadLine();
                        if (int.TryParse(npInput, out np) && np > 0)
                        {
                            validNp = true;
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Please enter a positive integer for the number of processes.");
                        }
                    }
                }

                // --- Switch based on user choice ---
                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\nSelected: FCFS Algorithm");
                        fcfsAlgorithm(np); // Call FCFS
                        break;

                    case "2":
                        Console.WriteLine("\nSelected: SRTF Algorithm");
                        ShortestTimeRemaining(np); // Call SRTF
                        break;

                    case "3":
                        Console.WriteLine("\nSelected: MLFQ Algorithm");
                        MultiLevelFeedbackQueue(np); // Call MLFQ
                        break;

                    case "0":
                        Console.WriteLine("Exiting program.");
                        break; // Exit the switch (and the loop condition will handle exit)

                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option from the menu.");
                        break;
                }

                // Pause after showing results (unless user chose to exit)
                if (choice != "0")
                {
                    Console.WriteLine("\nPress any key to return to the menu...");
                    Console.ReadKey();
                    Console.Clear(); // Optional: Clear console for the next menu display
                }

            } while (choice != "0"); // Continue until user chooses to exit

            Console.WriteLine("\nSimulation finished. Press any key to close the window.");
            Console.ReadKey();
        }


        // --- MLFQ Implementation ---
        public static void MultiLevelFeedbackQueue(int np)
        {
            if (np <= 0) { Console.WriteLine("Number of processes must be positive."); return; }

            List<MLFQProcess> allProcesses = new List<MLFQProcess>();
            List<MLFQProcess> completedProcesses = new List<MLFQProcess>();
            List<Queue<MLFQProcess>> queues = new List<Queue<MLFQProcess>>(NUM_QUEUES);
            for (int i = 0; i < NUM_QUEUES; i++)
            {
                queues.Add(new Queue<MLFQProcess>());
            }

            double totalBurstTimeSum = 0;

            // --- Input ---
            Console.WriteLine("\n--- Enter Process Details for MLFQ ---");
            for (int i = 0; i < np; i++)
            {
                double arrival = 0, burst = 0;
                Console.Write($"Enter Arrival Time for Process P{i + 1}: ");
                while (!double.TryParse(Console.ReadLine(), out arrival) || arrival < 0) { Console.WriteLine("Invalid input. Enter non-negative arrival time:"); }
                Console.Write($"Enter Burst Time for Process P{i + 1}: ");
                while (!double.TryParse(Console.ReadLine(), out burst) || burst <= 0) { Console.WriteLine("Invalid input. Enter positive burst time:"); }

                allProcesses.Add(new MLFQProcess(i + 1, arrival, burst));
                totalBurstTimeSum += burst;
            }
            allProcesses = allProcesses.OrderBy(p => p.ArrivalTime).ToList();
            int processesEnteredSystem = 0;


            // --- Simulation ---
            double currentTime = 0;
            double timeSinceLastBoost = 0;
            MLFQProcess currentProcess = null;
            int completedCount = 0;
            double lastCompletionTime = 0;
            int arrivalIndex = 0; // Track index for arriving processes

            Console.WriteLine("\n--- MLFQ Simulation Trace ---"); // Add trace header

            while (completedCount < np)
            {
                // ** 1. Priority Boost **
                if (BOOST_INTERVAL > 0 && timeSinceLastBoost >= BOOST_INTERVAL)
                {
                    Console.WriteLine($"--- Time {currentTime:F1}: Priority Boost ---");
                    // Temporarily store processes to move
                    List<MLFQProcess> processesToBoost = new List<MLFQProcess>();

                    // Check running process first
                    if (currentProcess != null)
                    {
                        Console.WriteLine($"   Boosting running P{currentProcess.ID} (from Q{currentProcess.CurrentQueueLevel}) to Q0");
                        processesToBoost.Add(currentProcess);
                        currentProcess = null; // Will need re-selection
                    }
                    // Check waiting queues (start from Q1)
                    for (int qLevel = 1; qLevel < NUM_QUEUES; qLevel++)
                    {
                        while (queues[qLevel].Count > 0)
                        {
                            MLFQProcess p = queues[qLevel].Dequeue();
                            Console.WriteLine($"   Boosting P{p.ID} from Q{qLevel} to Q0");
                            processesToBoost.Add(p);
                        }
                    }
                    // Add boosted processes to Q0 and reset state
                    foreach (var p in processesToBoost)
                    {
                        p.CurrentQueueLevel = 0;
                        p.TimeInCurrentQuantum = 0;
                        queues[0].Enqueue(p); // Add to the end of Q0
                    }
                    timeSinceLastBoost = 0; // Reset boost timer
                }


                // ** 2. Handle New Arrivals **
                bool newArrivalOccurred = false; // Flag to check if preemption check needed
                while (arrivalIndex < allProcesses.Count && allProcesses[arrivalIndex].ArrivalTime <= currentTime)
                {
                    MLFQProcess arrivingProcess = allProcesses[arrivalIndex];
                    arrivingProcess.CurrentQueueLevel = 0; // Arrives in highest queue
                    arrivingProcess.TimeInCurrentQuantum = 0;
                    queues[0].Enqueue(arrivingProcess);
                    Console.WriteLine($"--- Time {currentTime:F1}: P{arrivingProcess.ID} arrived, added to Q0 ---");
                    processesEnteredSystem++;
                    arrivalIndex++;
                    newArrivalOccurred = true; // Mark that an arrival happened
                }

                // ** Preemption Check (only if a new arrival happened *and* a process was running) **
                if (newArrivalOccurred && currentProcess != null)
                {
                    // Peek at Q0 to see if the new arrival is there (it should be)
                    // In MLFQ, *any* arrival into a higher queue preempts a lower queue process.
                    // Since arrivals go to Q0, if currentProcess is in Q1 or Q2, it *must* be preempted.
                    if (currentProcess.CurrentQueueLevel > 0) // Check if running process is in a lower queue
                    {
                        Console.WriteLine($"    Preempting P{currentProcess.ID} (Q{currentProcess.CurrentQueueLevel}) by new arrival in Q0");
                        // Put current process back to the *FRONT* of its queue (common preemption behavior)
                        // Alternatively, add to end: queues[currentProcess.CurrentQueueLevel].Enqueue(currentProcess);
                        // Let's add to front for fairness to preempted process:
                        var tempQueue = queues[currentProcess.CurrentQueueLevel].ToList();
                        tempQueue.Insert(0, currentProcess);
                        queues[currentProcess.CurrentQueueLevel] = new Queue<MLFQProcess>(tempQueue);

                        currentProcess = null; // Force re-selection
                    }
                }


                // ** 3. Select Process to Run **
                if (currentProcess == null)
                {
                    for (int qLevel = 0; qLevel < NUM_QUEUES; qLevel++)
                    {
                        if (queues[qLevel].Count > 0)
                        {
                            currentProcess = queues[qLevel].Dequeue();
                            Console.WriteLine($"--- Time {currentTime:F1}: Selecting P{currentProcess.ID} from Q{qLevel} ---");
                            if (!currentProcess.HasStarted)
                            {
                                currentProcess.StartTime = currentTime;
                                currentProcess.ResponseTime = currentTime - currentProcess.ArrivalTime;
                                if (currentProcess.ResponseTime < 0) currentProcess.ResponseTime = 0; // Safety
                                Console.WriteLine($"    P{currentProcess.ID} starting execution (RT: {currentProcess.ResponseTime:F2})");
                            }
                            break; // Found a process, stop searching queues
                        }
                    }
                }

                // ** 4. Execute or Handle Idle Time **
                if (currentProcess != null)
                {
                    double timeSliceForQueue = TIME_SLICES[currentProcess.CurrentQueueLevel];
                    double timeRemainingInQuantum = timeSliceForQueue - currentProcess.TimeInCurrentQuantum;
                    double timeToRun = Math.Min(currentProcess.RemainingBurstTime, timeRemainingInQuantum);

                    // Ensure timeToRun is positive
                    if (timeToRun <= 0)
                    {
                        // This might happen if RemainingBurstTime is 0 or negative due to precision, handle completion
                        timeToRun = 0; // Force check below
                    }
                    else
                    {
                        Console.WriteLine($"    Running P{currentProcess.ID} for up to {timeToRun:F1} units (RemBurst: {currentProcess.RemainingBurstTime:F1}, RemQuantum: {timeRemainingInQuantum:F1})");
                    }


                    // --- Simulate running for 'timeToRun' ---
                    // Edge case: If timeToRun is effectively zero but process isn't finished, handle demotion/requeue
                    if (timeToRun > 0)
                    {
                        currentProcess.RemainingBurstTime -= timeToRun;
                        currentProcess.TimeInCurrentQuantum += timeToRun;
                        currentTime += timeToRun;
                        timeSinceLastBoost += timeToRun;
                    }
                    else if (currentProcess.RemainingBurstTime > 0)
                    {
                        // If timeToRun is zero but burst remaining, it means quantum is used up.
                        // This happens if timeRemainingInQuantum was <= 0 at the start.
                        // Need to force the demotion/requeue logic.
                        Console.WriteLine($"    P{currentProcess.ID} time slice ({timeSliceForQueue}) expired immediately.");
                        // Fall through to the completion/quantum expiry checks.
                    }


                    // ** 5. Check Completion **
                    // Use a small tolerance for floating point comparisons
                    if (currentProcess.RemainingBurstTime < 0.00001)
                    {
                        // If completion time wasn't set precisely due to timeToRun being calculated slightly off
                        currentProcess.CompletionTime = currentTime; // Update completion time to be precise

                        Console.WriteLine($"--- Time {currentTime:F1}: P{currentProcess.ID} completed ---");
                        currentProcess.TurnaroundTime = currentProcess.CompletionTime - currentProcess.ArrivalTime;
                        currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.BurstTime;
                        if (currentProcess.WaitingTime < 0) currentProcess.WaitingTime = 0; // Safety

                        completedProcesses.Add(currentProcess);
                        completedCount++;
                        lastCompletionTime = currentTime;
                        currentProcess = null; // Mark CPU as free
                    }
                    // ** 6. Check Quantum Expiry (Demotion/Requeue) **
                    // Check if quantum is used up (within tolerance) AND process is not completed
                    else if (currentProcess.TimeInCurrentQuantum >= timeSliceForQueue - 0.00001)
                    {
                        int currentQ = currentProcess.CurrentQueueLevel;
                        int nextQ = Math.Min(currentQ + 1, NUM_QUEUES - 1); // Demote, but not beyond last queue

                        if (currentQ != nextQ)
                        { // Demote if not already in the last queue
                            currentProcess.CurrentQueueLevel = nextQ;
                            Console.WriteLine($"--- Time {currentTime:F1}: P{currentProcess.ID} quantum expired in Q{currentQ}. Demoting to Q{nextQ} ---");
                        }
                        else
                        {
                            // Process finished its slice in the lowest queue (FCFS part)
                            Console.WriteLine($"--- Time {currentTime:F1}: P{currentProcess.ID} finished run in Q{currentQ} (last queue). Re-adding to end ---");
                        }
                        currentProcess.TimeInCurrentQuantum = 0; // Reset quantum time
                        queues[currentProcess.CurrentQueueLevel].Enqueue(currentProcess); // Add to end of its (new) queue
                        currentProcess = null; // Mark CPU as free
                    }
                    // ** 7. Process Continues (Implicit) **
                    // If not completed and quantum not expired, 'currentProcess' remains set

                }
                else // No process is currently selected to run
                {
                    // Check if processes are still pending arrival
                    if (arrivalIndex < allProcesses.Count)
                    {
                        // Jump time to the next arrival if it's later than current time
                        double nextArrivalTime = allProcesses[arrivalIndex].ArrivalTime;
                        if (nextArrivalTime > currentTime)
                        {
                            Console.WriteLine($"--- Time {currentTime:F1}: CPU Idle. Jumping to next arrival at {nextArrivalTime:F1} ---");
                            timeSinceLastBoost += (nextArrivalTime - currentTime); // Account for idle time in boost timer
                            currentTime = nextArrivalTime;
                        }
                        else
                        {
                            // Edge case: Next arrival is <= current time, loop should handle it in next iteration
                            // To prevent potential infinite loop if logic is stuck, increment time slightly.
                            currentTime += 0.1; // Increment small amount if stuck waiting for arrival at current time
                            timeSinceLastBoost += 0.1;
                        }
                    }
                    // Check if all processes have arrived but queues are empty (implies all done)
                    else if (completedCount < np)
                    {
                        // All arrived, CPU idle, but not all complete? This indicates an error/stuck state.
                        Console.WriteLine($"--- Time {currentTime:F1}: CPU Idle, All processes arrived but not completed. Check Logic! Incrementing time. ---");
                        currentTime += 1.0; // Increment time to prevent potential infinite loop
                        timeSinceLastBoost += 1.0;
                    }
                    else
                    {
                        // All processes have arrived and all have completed. Exit loop.
                        break;
                    }
                }

            } // End while loop

            // --- Calculate and Output Metrics ---
            Console.WriteLine("\n--- MLFQ Final Results ---"); // Add header for final stats
                                                               // ... (rest of the metric calculation and output code is the same as previous MLFQ example) ...
            double totalWaitingTime = completedProcesses.Sum(p => p.WaitingTime);
            double totalTurnaroundTime = completedProcesses.Sum(p => p.TurnaroundTime);
            double totalResponseTime = completedProcesses.Sum(p => {
                // Ensure ResponseTime was calculated (StartTime != -1)
                if (p.StartTime != -1) return p.ResponseTime;
                else return 0; // Or handle as an error/unstarted process
            });

            double avgWaitingTime = (np == 0) ? 0 : totalWaitingTime / np;
            double avgTurnaroundTime = (np == 0) ? 0 : totalTurnaroundTime / np;
            double avgResponseTime = (np == 0) ? 0 : totalResponseTime / np;

            double totalTimeSpan = lastCompletionTime; // Time until the last process completed
            double cpuUtilization = (totalTimeSpan == 0) ? 0 : (totalBurstTimeSum / totalTimeSpan) * 100.0;
            double throughput = (totalTimeSpan == 0) ? 0 : np / totalTimeSpan;


            Console.WriteLine("\nProcess\tArrival\tBurst\tCompletion\tTurnaround\tWaiting\tResponse");
            Console.WriteLine("ID\tTime\tTime\tTime\t\tTime (TAT)\tTime (WT)\tTime (RT)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            completedProcesses = completedProcesses.OrderBy(p => p.ID).ToList();
            foreach (var p in completedProcesses)
            {
                Console.WriteLine($"P{p.ID}\t{p.ArrivalTime:F1}\t{p.BurstTime:F1}\t{p.CompletionTime:F1}\t\t{p.TurnaroundTime:F1}\t\t{p.WaitingTime:F1}\t\t{p.ResponseTime:F1}");
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            Console.WriteLine($"\nAverage Waiting Time (AWT)      = {avgWaitingTime:F2}");
            Console.WriteLine($"Average Turnaround Time (ATT)   = {avgTurnaroundTime:F2}");
            Console.WriteLine($"Average Response Time (RT)      = {avgResponseTime:F2}");
            Console.WriteLine($"CPU Utilization                 = {cpuUtilization:F2}%");
            Console.WriteLine($"Throughput                      = {throughput:F2} processes/time unit");
            Console.WriteLine($"Total Time Elapsed (Completion) = {lastCompletionTime:F2}");
            Console.WriteLine($"Boost Interval                  = {BOOST_INTERVAL}");
            Console.WriteLine($"Time Slices                     = Q0: {TIME_SLICES[0]}, Q1: {TIME_SLICES[1]}, Q2: FCFS");
        }


        //===========================================================
        //  fcfsAlgorithm 
        //===========================================================
        public static void fcfsAlgorithm(int np)
        {
            if (np <= 0)
            {
                Console.WriteLine("Number of processes must be positive.");
                return;
            }

            // --- Data Structures (same as before) ---
            int[] processId = new int[np];
            double[] arrivalTime = new double[np];
            double[] burstTime = new double[np];
            double[] completionTime = new double[np];
            double[] waitingTime = new double[np];
            double[] turnaroundTime = new double[np];
            double[] responseTime = new double[np];
            double[] startTime = new double[np]; // Keep calculating StartTime even if not printed in table

            double totalBurstTime = 0.0;
            double totalWaitingTime = 0.0;
            double totalTurnaroundTime = 0.0;
            double totalResponseTime = 0.0;
            const double TOLERANCE = 0.00001;

            // --- Input (same as before) ---
            Console.WriteLine("\n--- Enter Process Details for FCFS ---");
            for (int i = 0; i < np; i++)
            {
                processId[i] = i + 1;
                Console.Write($"Enter Arrival Time for Process P{processId[i]}: ");
                while (!double.TryParse(Console.ReadLine(), out arrivalTime[i]) || arrivalTime[i] < 0) { Console.WriteLine("Invalid input. Please enter a non-negative arrival time:"); }
                Console.Write($"Enter Burst Time for Process P{processId[i]}: ");
                while (!double.TryParse(Console.ReadLine(), out burstTime[i]) || burstTime[i] <= 0) { Console.WriteLine("Invalid input. Please enter a positive burst time:"); }
                totalBurstTime += burstTime[i];
            }

            // --- Sort Processes (with tie-breaking) ---
            var processesInfo = Enumerable.Range(0, np)
                                           .Select(i => new { OriginalIndex = i, Arrival = arrivalTime[i], ID = processId[i] })
                                           .OrderBy(p => p.Arrival)
                                           .ThenBy(p => p.ID)
                                           .ToList();

            // --- Simulation (same as before) ---
            double currentTime = 0.0;
            double lastCompletionTime = 0.0;
            double firstArrivalTime = (np > 0) ? processesInfo.First().Arrival : 0.0;

            foreach (var procInfo in processesInfo)
            {
                int i = procInfo.OriginalIndex;
                // Calculate all metrics, including StartTime
                startTime[i] = Math.Max(currentTime, arrivalTime[i]);
                completionTime[i] = startTime[i] + burstTime[i];
                turnaroundTime[i] = completionTime[i] - arrivalTime[i];
                waitingTime[i] = startTime[i] - arrivalTime[i];
                responseTime[i] = startTime[i] - arrivalTime[i];

                if (waitingTime[i] < TOLERANCE) waitingTime[i] = 0;
                if (responseTime[i] < TOLERANCE) responseTime[i] = 0;
                if (turnaroundTime[i] < TOLERANCE) turnaroundTime[i] = 0;

                currentTime = completionTime[i];
                lastCompletionTime = Math.Max(lastCompletionTime, completionTime[i]);
                totalWaitingTime += waitingTime[i];
                totalTurnaroundTime += turnaroundTime[i];
                totalResponseTime += responseTime[i];
            }

            // --- Calculate Final Metrics (same as before) ---
            double avgWaitingTime = (np == 0) ? 0 : totalWaitingTime / np;
            double avgTurnaroundTime = (np == 0) ? 0 : totalTurnaroundTime / np;
            double avgResponseTime = (np == 0) ? 0 : totalResponseTime / np;
            double activeTimeSpan = (np == 0) ? 0 : Math.Max(0, lastCompletionTime - firstArrivalTime);
            double calculationTimeSpan = (activeTimeSpan < TOLERANCE) ? lastCompletionTime : activeTimeSpan;
            double cpuUtilization = (calculationTimeSpan < TOLERANCE) ? 0 : (totalBurstTime / calculationTimeSpan) * 100.0;
            double throughput = (lastCompletionTime < TOLERANCE) ? 0 : np / lastCompletionTime;


            // --- Display Results (Matching SRTF Format) ---
            // *** MODIFIED OUTPUT FORMATTING HERE ***
            Console.WriteLine("\n--- FCFS Results ---");
            // Header lines matching SRTF example
            Console.WriteLine("Process\tArrival\tBurst\tCompletion\tTurnaround\tWaiting\tResponse");
            Console.WriteLine("ID\tTime\tTime\tTime\t\tTime (TAT)\tTime (WT)\tTime (RT)"); // Note: No "Start Time" column
                                                                                           // Separator Line matching SRTF example length
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            // Output results ordered by original process ID for consistency
            var outputOrder = Enumerable.Range(0, np).OrderBy(idx => processId[idx]);
            foreach (int i in outputOrder)
            {
                // Print data using Tabs, F1 format specifier, and matching double tabs
                // Note: startTime[i] is NOT printed here to match the SRTF column structure
                Console.WriteLine($"P{processId[i]}\t{arrivalTime[i]:F1}\t{burstTime[i]:F1}\t{completionTime[i]:F1}\t\t{turnaroundTime[i]:F1}\t\t{waitingTime[i]:F1}\t\t{responseTime[i]:F1}");
            }

            // Separator Line matching SRTF example length
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            // Print summary metrics (same as before)
            Console.WriteLine($"\nAverage Waiting Time (AWT)      = {avgWaitingTime:F2}"); // Keep F2 for averages
            Console.WriteLine($"Average Turnaround Time (ATT)   = {avgTurnaroundTime:F2}");
            Console.WriteLine($"Average Response Time (RT)      = {avgResponseTime:F2}");
            Console.WriteLine($"CPU Utilization (over active)   = {cpuUtilization:F2}%  (Span: {calculationTimeSpan:F2})");
            Console.WriteLine($"Throughput                      = {throughput:F2} processes/time unit (Total time: {lastCompletionTime:F2})");
            Console.WriteLine($"Total Time Elapsed (Completion) = {lastCompletionTime:F2}");
        }
        //===========================================================
        // Shortesttimeremaining
        //===========================================================
        public static void ShortestTimeRemaining(int np)
        {
            if (np <= 0)
            {
                Console.WriteLine("Number of processes must be positive.");
                return;
            }

            // --- Data Structures ---
            int[] processId = new int[np];
            double[] arrivalTime = new double[np];
            double[] burstTime = new double[np]; // Original burst time
            double[] remainingTime = new double[np];
            double[] completionTime = new double[np];
            double[] waitingTime = new double[np];
            double[] turnaroundTime = new double[np];
            double[] responseTime = new double[np];
            bool[] isCompleted = new bool[np];
            bool[] isResponseCalculated = new bool[np]; // Track if response time is set

            double totalWaitingTime = 0.0;
            double totalTurnaroundTime = 0.0;
            double totalResponseTime = 0.0;
            double totalBurstTime = 0.0;

            // --- Input ---
            Console.WriteLine("\n--- Enter Process Details for SRTF ---");
            for (int i = 0; i < np; i++)
            {
                processId[i] = i + 1; // Use 1-based indexing for user display

                Console.Write($"Enter Arrival Time for Process P{processId[i]}: ");
                while (!double.TryParse(Console.ReadLine(), out arrivalTime[i]) || arrivalTime[i] < 0)
                {
                    Console.WriteLine("Invalid input. Please enter a non-negative arrival time:");
                }

                Console.Write($"Enter Burst Time for Process P{processId[i]}: ");
                while (!double.TryParse(Console.ReadLine(), out burstTime[i]) || burstTime[i] <= 0) // Burst time must be positive
                {
                    Console.WriteLine("Invalid input. Please enter a positive burst time:");
                }

                remainingTime[i] = burstTime[i];
                totalBurstTime += burstTime[i];
                isCompleted[i] = false;
                isResponseCalculated[i] = false;
            }

            // --- Simulation ---
            int completedCount = 0;
            double currentTime = 0.0;
            double lastCompletionTime = 0.0; // Track the finish time of the last process
            const double TOLERANCE = 0.00001; // For floating point comparisons

            Console.WriteLine("\n--- SRTF Simulation Trace ---");

            while (completedCount < np)
            {
                // 1. Find the process with the shortest remaining time among arrived processes
                int shortestJobIndex = -1;
                double minRemainingTime = double.MaxValue;

                for (int i = 0; i < np; i++)
                {
                    if (arrivalTime[i] <= currentTime && !isCompleted[i])
                    {
                        if (remainingTime[i] < minRemainingTime)
                        {
                            minRemainingTime = remainingTime[i];
                            shortestJobIndex = i;
                        }
                        // Tie-breaking (optional but good): prefer process that arrived earlier
                        else if (remainingTime[i] == minRemainingTime)
                        {
                            if (shortestJobIndex == -1 || arrivalTime[i] < arrivalTime[shortestJobIndex])
                            {
                                shortestJobIndex = i;
                            }
                        }
                    }
                }

                // 2. Handle Idle Time
                if (shortestJobIndex == -1)
                {
                    // Find the earliest arrival time of a process that hasn't arrived yet
                    double nextArrivalTime = double.MaxValue;
                    for (int i = 0; i < np; i++)
                    {
                        if (!isCompleted[i] && arrivalTime[i] > currentTime)
                        {
                            nextArrivalTime = Math.Min(nextArrivalTime, arrivalTime[i]);
                        }
                    }

                    // If no process is ready and no future process exists, something's wrong or we are done (but completedCount<np says otherwise)
                    // If nextArrivalTime is still MaxValue here, it implies all remaining processes have arrived but aren't selectable (completed?)
                    // Or maybe all processes are completed, but the loop condition is wrong.
                    // Let's assume if nextArrivalTime remains MaxValue, we should break or signal error.
                    // However, the most likely scenario is there's a future arrival.
                    if (nextArrivalTime != double.MaxValue)
                    {
                        Console.WriteLine($"--- Time {currentTime:F1}: CPU Idle. Jumping to next arrival at {nextArrivalTime:F1} ---");
                        currentTime = nextArrivalTime; // Jump time to the next arrival
                    }
                    else
                    {
                        // Should not happen if completedCount < np means there are unfinished processes
                        // If it does, break to avoid infinite loop
                        Console.WriteLine($"--- Time {currentTime:F1}: CPU Idle. No ready jobs and no future arrivals. Exiting simulation loop. ---");
                        break; // Exit loop if no jobs are ready and none will arrive
                    }
                    continue; // Re-evaluate at the new current time
                }

                // 3. Process Execution
                int currentProcess = shortestJobIndex;
                Console.WriteLine($"--- Time {currentTime:F1}: Selecting P{processId[currentProcess]} (Rem: {remainingTime[currentProcess]:F1}) ---");

                // Calculate Response Time on first execution
                if (!isResponseCalculated[currentProcess])
                {
                    // Ensure response time isn't negative due to clock precision or jumping time
                    responseTime[currentProcess] = Math.Max(0, currentTime - arrivalTime[currentProcess]);
                    isResponseCalculated[currentProcess] = true;
                    totalResponseTime += responseTime[currentProcess];
                    Console.WriteLine($"    P{processId[currentProcess]} starting execution (RT: {responseTime[currentProcess]:F2})");
                }

                // Determine time until the next event (either current process completion or next arrival)
                double timeToCompletion = remainingTime[currentProcess];
                double timeToNextArrival = double.MaxValue;

                for (int i = 0; i < np; i++)
                {
                    if (!isCompleted[i] && arrivalTime[i] > currentTime)
                    {
                        timeToNextArrival = Math.Min(timeToNextArrival, arrivalTime[i]);
                    }
                }

                // Time slice to run: minimum of time needed to finish or time until next arrival
                double timeSlice = Math.Min(timeToCompletion, timeToNextArrival - currentTime);

                // Ensure timeSlice is positive to avoid issues if next arrival is exactly now
                if (timeSlice < TOLERANCE && timeToCompletion > TOLERANCE)
                {
                    // If the slice is tiny/zero because of an arrival happening NOW,
                    // but the process still needs time, we need to ensure the loop progresses.
                    // Let's allow a tiny execution or just proceed to re-evaluation in the next loop.
                    // Re-evaluating is safer. Let's slightly adjust timeSlice if it's due to completion.
                    if (timeToCompletion < TOLERANCE) timeSlice = timeToCompletion;
                    else timeSlice = TOLERANCE; // Minimum advance if arrival is immediate? Or just let next loop handle?
                                                // Let's try running for the calculated slice, even if small/zero.
                                                // If timeSlice is calculated as negative/zero due to next arrival == currentTime,
                                                // Math.Min handles it, resulting slice is timeToCompletion or 0.
                                                // A zero slice just means we immediately re-evaluate after potential completion check.
                }
                // Ensure timeSlice is not negative
                timeSlice = Math.Max(0, timeSlice);


                Console.WriteLine($"    Running P{processId[currentProcess]} for {timeSlice:F1} units.");

                // Update state
                remainingTime[currentProcess] -= timeSlice;
                currentTime += timeSlice;

                Console.WriteLine($"    P{processId[currentProcess]} New Rem: {remainingTime[currentProcess]:F1}");

                // 4. Check for Completion
                if (remainingTime[currentProcess] < TOLERANCE && !isCompleted[currentProcess]) // Use tolerance for completion check
                {
                    remainingTime[currentProcess] = 0; // Set exactly to 0 for clarity
                    completionTime[currentProcess] = currentTime;
                    turnaroundTime[currentProcess] = completionTime[currentProcess] - arrivalTime[currentProcess];
                    waitingTime[currentProcess] = turnaroundTime[currentProcess] - burstTime[currentProcess];

                    // Correct potential small negative values due to floating point math
                    if (waitingTime[currentProcess] < TOLERANCE) waitingTime[currentProcess] = 0;
                    if (turnaroundTime[currentProcess] < TOLERANCE) turnaroundTime[currentProcess] = 0;


                    totalTurnaroundTime += turnaroundTime[currentProcess];
                    totalWaitingTime += waitingTime[currentProcess];
                    isCompleted[currentProcess] = true;
                    completedCount++;
                    lastCompletionTime = currentTime; // Update last completion time

                    Console.WriteLine($"--- Time {currentTime:F1}: P{processId[currentProcess]} completed ---");
                    Console.WriteLine($"    (CT: {completionTime[currentProcess]:F1}, TAT: {turnaroundTime[currentProcess]:F1}, WT: {waitingTime[currentProcess]:F1})");
                }
                // No else needed, the loop will continue and re-evaluate which process is shortest
            } // End while loop

            // --- Calculate & Display Results ---
            double avgWaitingTime = (np == 0) ? 0 : totalWaitingTime / np;
            double avgTurnaroundTime = (np == 0) ? 0 : totalTurnaroundTime / np;
            double avgResponseTime = (np == 0) ? 0 : totalResponseTime / np;

            // Total time span is the time the last process finished
            double totalTimeSpan = lastCompletionTime;
            // Handle case where totalTimeSpan might be 0 if all processes had 0 burst time (though input validation prevents this)
            // Or if no processes were run. Better: use max completion time or max arrival+burst?
            // Using lastCompletionTime is standard for calculating utilization based on when the system finished working.
            // If the system was idle at the start, that time isn't typically included in the span for utilization.
            // Let's refine the span start from the first arrival time if needed.
            double firstArrivalTime = np > 0 ? arrivalTime.Min() : 0;
            totalTimeSpan = Math.Max(0, lastCompletionTime - firstArrivalTime); // Span from first arrival to last completion
            if (totalTimeSpan < TOLERANCE) // Avoid division by zero if span is effectively zero
            {
                totalTimeSpan = lastCompletionTime; // Fallback to simple last completion time if needed
            }

            double cpuUtilization = (totalTimeSpan < TOLERANCE) ? 0 : (totalBurstTime / (lastCompletionTime - firstArrivalTime)) * 100.0; // Utilization over the active period
                                                                                                                                          // Adjust CPU Utilization denominator if system was idle before first arrival
            double throughput = (lastCompletionTime < TOLERANCE) ? 0 : np / lastCompletionTime; // Processes completed per unit time from t=0


            Console.WriteLine("\n--- SRTF Results ---");
            Console.WriteLine("Process\tArrival\tBurst\tCompletion\tTurnaround\tWaiting\tResponse");
            Console.WriteLine("ID\tTime\tTime\tTime\t\tTime (TAT)\tTime (WT)\tTime (RT)");
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            // Sort by Process ID for consistent output
            var outputOrder = Enumerable.Range(0, np).OrderBy(i => processId[i]);
            foreach (int i in outputOrder)
            {
                Console.WriteLine($"P{processId[i]}\t{arrivalTime[i]:F1}\t{burstTime[i]:F1}\t{completionTime[i]:F1}\t\t{turnaroundTime[i]:F1}\t\t{waitingTime[i]:F1}\t\t{responseTime[i]:F1}");
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine($"\nAverage Waiting Time (AWT)      = {avgWaitingTime:F2}");
            Console.WriteLine($"Average Turnaround Time (ATT)   = {avgTurnaroundTime:F2}");
            Console.WriteLine($"Average Response Time (ART)     = {avgResponseTime:F2}"); // Renamed for consistency
            Console.WriteLine($"CPU Utilization                 = {cpuUtilization:F2}%");
            Console.WriteLine($"Throughput                      = {throughput:F2} processes/time unit");
            Console.WriteLine($"Total Time Elapsed (Completion) = {lastCompletionTime:F2}");
        }


    }
} 
