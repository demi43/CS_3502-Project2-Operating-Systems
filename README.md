# OPERATING SYSTEM PROJECT 
# CPU Scheduling Algorithm Simulator

This C# console application simulates several common CPU scheduling algorithms:

* **First-Come, First-Served (FCFS)**: Processes are executed in the order they arrive.
* **Shortest Remaining Time First (SRTF)**: The process with the smallest remaining burst time is executed next. This is a preemptive algorithm.
* **Multi-Level Feedback Queue (MLFQ)**: A more complex algorithm that uses multiple priority queues to manage processes. Processes can move between queues based on their CPU usage.

## How to Run the Application

1.  **Save the Code**: Save the provided C# code as a `.cs` file (e.g., `SchedulingSimulator.cs`).
2.  **Compile**: Open a command prompt or terminal in the directory where you saved the file and use the C# compiler (`csc`) to compile the code:
    ```bash
    csc SchedulingSimulator.cs
    ```
    This will create an executable file (e.g., `SchedulingSimulator.exe`).
3.  **Execute**: Run the executable from the command prompt or terminal:
    ```bash
    SchedulingSimulator.exe
    ```
4.  **Follow the Menu**: The application will display a menu allowing you to choose which scheduling algorithm to simulate.
5.  **Enter Process Details**: For the selected algorithm, you will be prompted to enter the number of processes and then the arrival time and burst time for each process.
6.  **View Results**: After the simulation, the application will display a trace of the execution (for SRTF and MLFQ) and the final results, including process-specific metrics and average metrics.

## Algorithm Details

### 1. First-Come, First-Served (FCFS)

* **Description**: FCFS is the simplest scheduling algorithm. Processes are served in the exact order they arrive in the ready queue. Once a process starts executing, it continues until it completes its burst time.
* **Implementation Notes**:
    * Processes are ordered based on their arrival times.
    * The simulation maintains the current time and processes each process in the sorted order.
    * Waiting time for a process is the time it spends in the ready queue before its execution begins.
    * Turnaround time is the total time from arrival to completion.
    * Response time is the time from arrival to the first time the process gets the CPU. For FCFS, this is the same as the waiting time (or 0 if it's the first process).
* **Output**: The results include the arrival time, burst time, start time, completion time, turnaround time, waiting time, and response time for each process, as well as the average waiting time, average turnaround time, average response time, CPU utilization, and throughput.

### 2. Shortest Remaining Time First (SRTF)

* **Description**: SRTF is a preemptive version of the Shortest Job First (SJF) algorithm. The process with the smallest remaining burst time is executed next. If a new process arrives with a burst time shorter than the remaining time of the currently executing process, the current process is preempted, and the new process takes over the CPU.
* **Implementation Notes**:
    * The simulation tracks the remaining burst time for each process.
    * At each time step, it selects the process with the minimum remaining burst time among those that have arrived and are not yet completed.
    * If a new process arrives with a shorter remaining burst time than the currently running process, a preemption occurs.
    * Response time is recorded when a process first starts its execution.
* **Output**: The simulation trace shows the selection of processes at different time units and any preemptions. The final results include the arrival time, burst time, completion time, turnaround time, waiting time, and response time for each process, along with the average metrics, CPU utilization, and throughput.

### 3. Multi-Level Feedback Queue (MLFQ)

* **Description**: MLFQ uses multiple priority queues with different scheduling algorithms at each level. Processes are assigned to the highest priority queue upon arrival. If a process uses its entire time slice in a queue, its priority is reduced (it's moved to a lower priority queue). This aims to favor short, interactive processes. A priority boost mechanism is often used to prevent starvation.
* **Implementation Details**:
    * **Number of Queues**: 3 (Q0, Q1, Q2).
    * **Scheduling in Each Queue**:
        * Q0: Round Robin (RR) with a time slice of 8 units.
        * Q1: Round Robin (RR) with a time slice of 16 units.
        * Q2: First-Come, First-Served (FCFS) (effectively infinite time slice).
    * **Priority Boost**: All processes are moved to the highest priority queue (Q0) every 100 time units.
    * **Demotion**: If a process uses its entire time slice in Q0 or Q1 without completing, it is moved to the next lower priority queue.
    * **Arrival Preemption**: A process arriving at Q0 will preempt a currently running process from a lower priority queue (Q1 or Q2). The preempted process is moved to the front of its original queue.
* **Output**: The simulation trace shows process arrivals, selections, running times, quantum expirations, demotions, priority boosts, and completions. The final results include the standard process metrics and average metrics, as well as the MLFQ specific parameters (Boost Interval, Time Slices).
