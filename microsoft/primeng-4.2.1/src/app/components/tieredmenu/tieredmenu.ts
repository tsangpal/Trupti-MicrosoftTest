import {NgModule,Component,ElementRef,AfterViewInit,OnDestroy,Input,Output,Renderer2} from '@angular/core';
import {CommonModule} from '@angular/common';
import {DomHandler} from '../dom/domhandler';
import {MenuItem} from '../common/menuitem';
import {RouterModule} from '@angular/router';

@Component({
    selector: 'p-tieredMenuSub',
    template: `
        <ul [ngClass]="{'ui-helper-reset':root, 'ui-widget-content ui-corner-all ui-helper-clearfix ui-menu-child ui-shadow':!root}" class="ui-menu-list"
            (click)="listClick($event)">
            <ng-template ngFor let-child [ngForOf]="(root ? item : item.items)">
                <li *ngIf="child.separator" class="ui-menu-separator ui-widget-content">
                <li *ngIf="!child.separator" #listItem [ngClass]="{'ui-menuitem ui-widget ui-corner-all':true,'ui-menu-parent':child.items,'ui-menuitem-active':listItem==activeItem}"
                    (mouseenter)="onItemMouseEnter($event, listItem, child)" (mouseleave)="onItemMouseLeave($event)">
                    <a *ngIf="!child.routerLink" [href]="child.url||'#'" class="ui-menuitem-link ui-corner-all" [attr.target]="child.target" [attr.title]="child.title"
                        [ngClass]="{'ui-state-disabled':child.disabled}" (click)="itemClick($event, child)">
                        <span class="ui-submenu-icon fa fa-fw fa-caret-right" *ngIf="child.items"></span>
                        <span class="ui-menuitem-icon fa fa-fw" *ngIf="child.icon" [ngClass]="child.icon"></span>
                        <span class="ui-menuitem-text">{{child.label}}</span>
                    </a>
                    <a *ngIf="child.routerLink" [routerLink]="child.routerLink" [routerLinkActive]="'ui-state-active'" 
                        [routerLinkActiveOptions]="child.routerLinkActiveOptions||{exact:false}" [href]="child.url||'#'" 
                        class="ui-menuitem-link ui-corner-all" [attr.target]="child.target" [attr.title]="child.title"
                        [ngClass]="{'ui-state-disabled':child.disabled}" (click)="itemClick($event, child)">
                        <span class="ui-submenu-icon fa fa-fw fa-caret-right" *ngIf="child.items"></span>
                        <span class="ui-menuitem-icon fa fa-fw" *ngIf="child.icon" [ngClass]="child.icon"></span>
                        <span class="ui-menuitem-text">{{child.label}}</span>
                    </a>
                    <p-tieredMenuSub class="ui-submenu" [item]="child" *ngIf="child.items"></p-tieredMenuSub>
                </li>
            </ng-template>
        </ul>
    `,
    providers: [DomHandler]
})
export class TieredMenuSub {

    @Input() item: MenuItem;
    
    @Input() root: boolean;
    
    constructor(public domHandler: DomHandler) {}
    
    activeItem: HTMLLIElement;
                
    onItemMouseEnter(event: Event, item: HTMLLIElement, menuitem: MenuItem) {
        if(menuitem.disabled) {
            return;
        }
        
        this.activeItem = item;
        let nextElement:  HTMLElement =  <HTMLElement> item.children[0].nextElementSibling;
        if(nextElement) {
            let sublist:  HTMLElement = <HTMLElement> nextElement.children[0];
            sublist.style.zIndex = String(++DomHandler.zindex);
                        
            sublist.style.top = '0px';
            sublist.style.left = this.domHandler.getOuterWidth(item.children[0]) + 'px';
        }
    }
    
    onItemMouseLeave(event: Event) {
        this.activeItem = null;
    }
    
    itemClick(event: Event, item: MenuItem)??{
        if(item.disabled) {
            event.preventDefault();
            return true;
        }
        
        if(!item.url) {
            event.preventDefault();
        }
        
        if(item.command) {            
            item.command({
                originalEvent: event,
                item: item
            });
        }
    }
    
    listClick(event: Event) {
        this.activeItem = null;
    }
}

@Component({
    selector: 'p-tieredMenu',
    template: `
        <div [ngClass]="{'ui-tieredmenu ui-menu ui-widget ui-widget-content ui-corner-all ui-helper-clearfix':true,'ui-menu-dynamic ui-shadow':popup}" 
            [class]="styleClass" [ngStyle]="style">
            <p-tieredMenuSub [item]="model" root="root"></p-tieredMenuSub>
        </div>
    `,
    providers: [DomHandler]
})
export class TieredMenu implements AfterViewInit,OnDestroy {

    @Input() model: MenuItem[];

    @Input() popup: boolean;

    @Input() style: any;

    @Input() styleClass: string;
    
    @Input() appendTo: any;
    
    container: any;
    
    documentClickListener: any;
    
    preventDocumentDefault: any;
    
    constructor(public el: ElementRef, public domHandler: DomHandler, public renderer: Renderer2) {}

    ngAfterViewInit() {
        this.container = this.el.nativeElement.children[0];
        
        if(this.popup) {
            if(this.appendTo) {
                if(this.appendTo === 'body')
                    document.body.appendChild(this.container);
                else
                    this.domHandler.appendChild(this.container, this.appendTo);
            }
        }
    }
    
    toggle(event: Event) {
        if(this.container.offsetParent)
            this.hide();
        else
            this.show(event);
    }
    
    show(event: Event) {
        this.preventDocumentDefault = true;
        this.container.style.display = 'block';
        this.domHandler.absolutePosition(this.container, event.currentTarget);
        this.domHandler.fadeIn(this.container, 250);
        this.bindDocumentClickListener();
    }
    
    hide() {
        this.container.style.display = 'none';
        this.unbindDocumentClickListener();
    }
    
    unbindDocumentClickListener() {
        if(this.documentClickListener) {
            this.documentClickListener();
            this.documentClickListener = null;
        }
    }
    
    bindDocumentClickListener() {
        if(!this.documentClickListener) {
            this.documentClickListener = this.renderer.listen('document', 'click', () => {
                if(!this.preventDocumentDefault) {
                    this.hide();
                }
                this.preventDocumentDefault = false;
            });
        }
    }

    ngOnDestroy() {
        if(this.popup && this.documentClickListener) {
            this.unbindDocumentClickListener();
            
            if(this.appendTo) {
                this.el.nativeElement.appendChild(this.container);
            }
        }
    }

}

@NgModule({
    imports: [CommonModule,RouterModule],
    exports: [TieredMenu,RouterModule],
    declarations: [TieredMenu,TieredMenuSub]
})
export class TieredMenuModule { }
