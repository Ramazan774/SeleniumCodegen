Feature: ToDoApp

Scenario: Perform recorded actions on ToDoApp
	Given I navigate to "https://todomvc.com/examples/react/dist/"
	And I press Enter in element with Id "todo-input"
	And I press Enter in element with Id "todo-input"
	And I press Enter in element with Id "todo-input"
	And I type "on" into element with ClassName "toggle"
	When I click the element with ClassName "toggle"
	Then the page should be in the expected state
