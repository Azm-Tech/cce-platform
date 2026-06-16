import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Input,
  OnInit,
  forwardRef,
  inject,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
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
    <div
      class="cce-rte__wrapper"
      [class.cce-rte--disabled]="isDisabled"
      [class.cce-rte--rtl]="dir === 'rtl'"
      [class.cce-rte--ltr]="dir === 'ltr'"
    >
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
      @if (maxLength) {
        <span class="cce-rte__counter" [class.cce-rte__counter--over]="value.length > maxLength">
          {{ value.length }} / {{ maxLength }}
        </span>
      }
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

    /* Char counter — counts the HTML payload length, matching the
       server-side limit which validates the raw string. */
    .cce-rte__counter {
      align-self: flex-end;
      font-size: 11px;
      color: rgba(0, 0, 0, 0.55);
      padding: 0 2px;
    }

    .cce-rte__counter--over {
      color: var(--danger--600);
      font-weight: 600;
    }

    /* Match Material outline field border colour and radius */
    :host ::ng-deep .ql-toolbar.ql-snow {
      border: 1px solid rgba(0, 0, 0, 0.38);
      border-bottom: none;
      border-radius: 4px 4px 0 0;
      background: var(--neutrals--50);
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

    /* Forced content direction — overrides the document direction so an
       Arabic-content field is RTL even in an LTR UI and vice versa. */
    .cce-rte--rtl ::ng-deep .ql-editor {
      direction: rtl;
      text-align: right;
    }

    .cce-rte--ltr ::ng-deep .ql-editor {
      direction: ltr;
      text-align: left;
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
export class RichTextEditorComponent implements ControlValueAccessor, OnInit {
  @Input() label = '';
  @Input() placeholder = '';
  /** When set, shows a live character counter (counts the HTML string,
   *  which is what the backend validates against). Pair with a
   *  Validators.maxLength on the form control to actually block submit. */
  @Input() maxLength?: number;

  /** Force the editor CONTENT direction regardless of the page locale —
   *  e.g. dir="rtl" for an Arabic-content field inside an English UI,
   *  or dir="ltr" for an English-content field inside an Arabic UI.
   *  Unset = inherit from the document. */
  @Input() dir?: 'rtl' | 'ltr';

  private readonly cdr = inject(ChangeDetectorRef);
  private readonly doc = inject(DOCUMENT);

  readonly modules: QuillModules = BASIC_TOOLBAR;

  /** Quill's stylesheet is shipped as a lazy `quill` style bundle (see
   *  project.json) instead of the global render-blocking bundle. Inject it
   *  once, the first time any editor instance mounts. (PERF001 A1) */
  ngOnInit(): void {
    const id = 'cce-quill-css';
    if (this.doc.getElementById(id)) return;
    const link = this.doc.createElement('link');
    link.id = id;
    link.rel = 'stylesheet';
    link.href = 'quill.css';
    this.doc.head.appendChild(link);
  }

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
