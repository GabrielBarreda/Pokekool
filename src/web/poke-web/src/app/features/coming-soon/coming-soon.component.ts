import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { inject } from '@angular/core';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  imports: [EmptyStateComponent],
  template: `
    <app-empty-state
      icon="🚧"
      [title]="featureName + ' — En construction'"
      description="Cette section sera disponible prochainement."
    />
  `,
})
export class ComingSoonComponent {
  private readonly route = inject(ActivatedRoute);
  get featureName(): string {
    return this.route.snapshot.data['feature'] ?? 'Cette fonctionnalité';
  }
}
