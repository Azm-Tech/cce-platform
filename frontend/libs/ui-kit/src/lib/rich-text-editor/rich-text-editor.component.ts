import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Input,
  forwardRef,
  inject,
} from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { QuillModule, type QuillModules } from 'ngx-quill';
import type { ContentChange } from 'ngx-quill';

const BASIC_TOOLBAR: QuillModules = {
  toolbar: [
    [{ header: [1, 2, 3, false] }],
    ['bold', 'italic', 'underline'],
    [{ list: 'ordered' }, { list: 'bullet' }],
    ['link'],
    ['clean'],
  ],
};

@Component({
  selector: 'cce-rich-text-editor',
  standalone: true,
  imports: [QuillModule, FormsModule],
  template: `
    <div class="cce-rte__wrapper" [class.cce-rte--disabled]="isDisabled">
      @if (label) {
        <label class="cce-rte__label">{{ label }}</label>
      }
      <quill-editor
        [modules]="modules"
        [placeholder]="placeholder"
        [readOnly]="isDisabled"
        [ngModel]="value"
        (onContentChanged)="onContentChanged($event)"
        (onBlur)="onTouched()"
        format="html"
      />
    </div>
  `,
  styles: [`
    :host { display: block; }

    .cce-rte__wrapper {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .cce-rte__label {
      font-size: 12px;
      color: rgba(0, 0, 0, 0.6);
      padding: 0 2px;
      font-family: inherit;
    }

    /* Match Material outline field border colour and radius */
    :host ::ng-deep .ql-toolbar.ql-snow {
      border: 1px solid rgba(0, 0, 0, 0.38);
      border-bottom: none;
      border-radius: 4px 4px 0 0;
      background: #fafafa;
      font-family: inherit;
    }

    :host ::ng-deep .ql-container.ql-snow {
      border: 1px solid rgba(0, 0, 0, 0.38);
      border-radius: 0 0 4px 4px;
      font-size: 14px;
      font-family: inherit;
    }

    :host ::ng-deep .ql-editor {
      min-height: 120px;
      padding: 10px 12px;
      line-height: 1.6;
    }

    :host ::ng-deep .ql-editor.ql-blank::before {
      color: rgba(0, 0, 0, 0.42);
      font-style: normal;
    }

    /* Hover — deepen border like Material does */
    :host:hover ::ng-deep .ql-toolbar.ql-snow,
    :host:hover ::ng-deep .ql-container.ql-snow {
      border-color: rgba(0, 0, 0, 0.87);
    }

    /* Disabled state */
    .cce-rte--disabled ::ng-deep .ql-toolbar.ql-snow,
    .cce-rte--disabled ::ng-deep .ql-container.ql-snow {
      border-color: rgba(0, 0, 0, 0.12);
      background: rgba(0, 0, 0, 0.04);
    }

    .cce-rte--disabled ::ng-deep .ql-editor {
      color: rgba(0, 0, 0, 0.38);
      cursor: not-allowed;
    }
  `],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RichTextEditorComponent),
      multi: true,
    },
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RichTextEditorComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '';

  private readonly cdr = inject(ChangeDetectorRef);

  readonly modules: QuillModules = BASIC_TOOLBAR;

  value = '';
  isDisabled = false;

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  onChange: (v: string) => void = () => {};
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  onTouched: () => void = () => {};

  onContentChanged(event: ContentChange): void {
    const html = event.html ?? '';
    this.value = html;
    this.onChange(html);
  }

  writeValue(value: string): void {
    this.value = value ?? '';
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (v: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
    this.cdr.markForCheck();
  }
}
