# TelegraphSearchEngine

` The text below contains inaccuracies, due to laziness the text was generated by google bard `

This WPF application utilizes the Model-View-ViewModel (MVVM) pattern to provide a user-friendly interface for searching for articles. The application allows users to select an article title and language, and then presses the start button to initiate an asynchronous search for the article from a 200-answer URL. The results of the search are then displayed to the user.

### Prerequisites:

- Visual Studio 2019 or higher
- C# and XAML development experience

### Setup:
- Clone the project repository from GitHub.
- Open the project in Visual Studio.
- Build the project.
- In the Solution Explorer, right-click on the project node and select Run.

### Files:
- MainWindow.xaml: This file defines the main window of the application, including the user interface elements such as the text boxes for title and language selection, the start button, and the result text box.

- MainViewModel.cs: This class defines the view model, which acts as the intermediary between the view (MainWindow.xaml) and the model. It contains the logic for handling user interactions, such as selecting a title and language, and initiating the article search.

- UrlFunctions: This class represents the model, which encapsulates the data related to the article search, such as the title, language, and search results. It provides methods for searching for the article from the 200-answer URL.

### Basic logic:

The user selects an article title and language in the text boxes.
When the user presses the start button, the MainWindow sends a command to the view model to initiate the article search.
The view model retrieves the selected title and language from the view and passes them to the ArticleSearchModel.
The ArticleSearchModel makes an asynchronous HTTP request to the 200-answer URL to search for the article.
The server responds with a list of possible answers.
The ArticleSearchModel parses the response and extracts the relevant information from the possible answers.
The view model updates the result text box in the MainWindow with the extracted information.
