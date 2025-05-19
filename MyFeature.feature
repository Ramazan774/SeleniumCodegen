Feature: MyFeature

Scenario: Perform recorded actions on MyFeature
	Given I navigate to "https://todomvc.com/examples/react/dist/"
	And I type "buy grocery" and press Enter in element with data-testid "text-input"
	And I type "go to gym" and press Enter in element with data-testid "text-input"
	And I type "study js" and press Enter in element with data-testid "text-input"
	And I type "play with cat" and press Enter in element with data-testid "text-input"
	When I click the element with data-testid "todo-item-toggle"
	Then the page should be in the expected state
