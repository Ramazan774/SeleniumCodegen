Feature: MyFeature

Scenario: Perform recorded actions on MyFeature
	Given I navigate to "https://todomvc.com/examples/react/dist/"
	And I type "buy groceries" and press Enter in element with data-test-id "text-input"
	And I type "go to gym" and press Enter in element with data-test-id "text-input"
	And I type "study csharp" and press Enter in element with data-test-id "text-input"
	When I click the element with data-test-id "todo-item-toggle"
	Then the page should be in the expected state
