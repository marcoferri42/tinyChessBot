import matplotlib.pyplot as plt
import numpy as np
import seaborn as sns  # Seaborn is used for KDE
from datetime import datetime

today = datetime.now()

def read_data_from_file(file_path):
    data = []
    with open(file_path, 'r') as file:
        for line in file:
            # Split the line by comma, filter out empty strings, and convert to integers
            line_data = [int(x) for x in line.strip().split(',') if x.strip().isdigit()]
            data.extend(line_data)
    return data

def pair_read_data_from_file(file_path):
    data_pairs = []
    with open(file_path, 'r') as file:
        for line in file:
            # Split the line by comma and filter out any empty strings
            elements = [x for x in line.strip().split(',') if x]
            # Check if we have an even number of elements before proceeding
            if len(elements) % 2 != 0:
                raise ValueError(f"Expected an even number of elements in line: {line.strip()}")
            # Group the elements into pairs
            pairs = [(float(elements[i]), float(elements[i+1])) for i in range(0, len(elements), 2)]
            data_pairs.extend(pairs)
    return data_pairs

# Function to plot a histogram of the given data
def plot_histogram(data):
    plt.hist(data, bins='auto', alpha=0.7, color='blue', edgecolor='black')
    plt.title('Histogram of Data')
    plt.xlabel('Value')
    plt.ylabel('Frequency')
    plt.show()

def plot_response_time_distribution(data):
    plt.figure(figsize=(12, 8))  # Set a larger figure size

    # Histogram with a suitable number of bins
    num_bins = min(50, int(np.sqrt(len(data))))  # Rule-of-thumb for number of bins
    plt.hist(data, bins=num_bins, alpha=0.5, color='blue', edgecolor='black', label='Histogram')

    # Kernel Density Estimate plot
    sns.kdeplot(data, color='red', label='KDE')

    # Log Transformation if your data is skewed
    if all(x > 0 for x in data):  # Check if data is suitable for log transformation
        log_data = np.log(data)
        plt.hist(log_data, bins=num_bins, alpha=0.5, color='green', edgecolor='black', label='Log-transformed Histogram')
        sns.kdeplot(log_data, color='darkgreen', label='Log-transformed KDE')

    # CDF plot
    sorted_data = np.sort(data)
    yvals = np.arange(len(sorted_data)) / float(len(sorted_data) - 1)
    plt.plot(sorted_data, yvals, label='CDF', color='magenta')

    plt.title('Response Time Distribution')
    plt.xlabel('Response Time')
    plt.ylabel('Frequency / Probability')
    plt.xscale('log')  # Set the x-axis to a log scale
    plt.grid(True, which="both", ls="--", linewidth=0.5)  # Add grid for better readability
    plt.legend()
    plt.tight_layout()  # Adjust the layout to fit all labels
    plt.show()

def plot_detailed_histogram(data):
    plt.figure(figsize=(10, 6))  # Larger figure size for better readability

    # Histogram with increased number of bins
    plt.hist(data, bins=50, alpha=0.5, color='blue', edgecolor='black', label='Histogram')

    # Kernel Density Estimate plot
    sns.kdeplot(data, color='red', label='KDE')

    # Log transformation if necessary
    if any(x <= 0 for x in data):
        print("Data contains non-positive values, cannot apply log transformation.")
    else:
        log_data = np.log(data)
        plt.hist(log_data, bins=50, alpha=0.5, color='green', edgecolor='black', label='Log-transformed Histogram')
        sns.kdeplot(log_data, color='darkgreen', label='Log-transformed KDE')

    plt.title('Detailed Histogram and KDE')
    plt.xlabel('Value')
    plt.ylabel('Frequency')
    plt.legend()
    plt.show()

def plot_data_pairs(data_pairs):
    # Unzip the list of tuples into two separate lists
    A_values, B_values = zip(*data_pairs)

    plt.scatter(B_values, A_values, alpha=0.25)
    plt.suptitle(str(today))
    plt.title('Find move at depth '+str(input('depth:'))+', '+str(input('moves:'))+' moves')
    plt.ylabel('Response Time (ms)')
    plt.xlabel('Complexity (n possible branches)')
    plt.show()



# Main function to execute the program
def main():
    file_path = "C:\\Users\\usr\\source\\repos\\tinyChessBot\\Chess-Challenge\\src\\My Bot\\logsresponsetime4log.txt"
    data = pair_read_data_from_file(file_path)
    plot_data_pairs(data)

if __name__ == "__main__":
    main()