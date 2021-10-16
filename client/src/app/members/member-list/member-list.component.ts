import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { Member } from 'src/app/_models/member';
import { AccountService } from 'src/app/_services/account.service';
import { MembersService } from 'src/app/_services/members.service';

@Component({
  selector: 'app-member-list',
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.css']
})
export class MemberListComponent implements OnInit {

  members$: Observable<Member[]> = new Observable<Member[]>();


  constructor(private accountService: AccountService, private memberService: MembersService) { }


  current: string = "";

  ngOnInit(): void {
    this.members$ = this.memberService.getMembers();
  }

}
