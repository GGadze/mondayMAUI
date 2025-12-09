using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp4.DTO;
using MauiApp4.Model;
using MauiApp4.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp4.ViewModel
{
#pragma warning disable
    partial class ContactsViewModel : ObservableObject
    {
        private readonly IApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<ContactDto> _contacts = new ObservableCollection<ContactDto>();
        [ObservableProperty]
        private ContactDto _selectedContact;
        [ObservableProperty]
        private string _searchText;
        [ObservableProperty]
        private bool _isRefreshing;
        [ObservableProperty]
        private bool _isModalVisible;
        [ObservableProperty]
        private ContactDto _editingContact;
        [ObservableProperty]
        private bool _isBusy;
        [ObservableProperty]
        private string _modalTitle;



        public ContactsViewModel(IApiService apiserv)
        {
            _apiService = apiserv;
        }


        public ContactsViewModel() 
        {
            _apiService = new ApiService();
            Task.Run(async () => await LoadContacts());
        }

        [RelayCommand]
        private async Task LoadContacts()
        {
            try
            {
                IsBusy = true;
                var list = await _apiService.GetContactsAsync();
                Contacts.Clear();
                foreach (var c in list)
                    Contacts.Add(c);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"ошибка загрузки данных {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void AddContact()
        {
            EditingContact = new ContactDto();
            ModalTitle = "Создать контакт";
            IsModalVisible = true;
        }

        [RelayCommand]
        private void EditContact(ContactDto contact)
        {
            if (contact == null) return;

            EditingContact = new ContactDto
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Phone = contact.Phone,
                Email = contact.Email,
                Address = contact.Address
            };

            ModalTitle = "Редактировать контакт";
            IsModalVisible = true;
        }


        [RelayCommand]
        private async Task DeleteContact(ContactDto contact)
        {
            if (contact == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Удалить?",
                $"Удалить контакт {contact.FirstName} {contact.LastName}?",
                "Да", "Нет"
            );

            if (!confirm) return;

            try
            {
                await _apiService.DeleteContactAsync(contact.Id);
                Contacts.Remove(contact);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "ОК");
            }
        }

        [RelayCommand]
        private async Task SaveContact()
        {
            if (EditingContact == null)
                return;

            try
            {
                if (EditingContact.Id == 0)
                {
                    var createDto = new CreateContactDto
                    {
                        FirstName = EditingContact.FirstName,
                        LastName = EditingContact.LastName,
                        Phone = EditingContact.Phone,
                        Email = EditingContact.Email,
                        Address = EditingContact.Address
                    };

                    var created = await _apiService.CreateContactAsync(createDto);
                    Contacts.Add(created);
                }
                else
                {
                    var updateDto = new UpdateContactDto
                    {
                        FirstName = EditingContact.FirstName,
                        LastName = EditingContact.LastName,
                        Phone = EditingContact.Phone,
                        Email = EditingContact.Email,
                        Address = EditingContact.Address
                    };

                    await _apiService.UpdateContactAsync(EditingContact.Id, updateDto);

                    var existing = Contacts.FirstOrDefault(c => c.Id == EditingContact.Id);
                    if (existing != null)
                    {
                        existing.FirstName = EditingContact.FirstName;
                        existing.LastName = EditingContact.LastName;
                        existing.Phone = EditingContact.Phone;
                        existing.Email = EditingContact.Email;
                        existing.Address = EditingContact.Address;
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                EditingContact = null;
                IsModalVisible = false;
                OnPropertyChanged(nameof(Contacts));
            }
        }



        [RelayCommand]
        private async Task RefreshContacts()
        {
            IsRefreshing = true;
            var contacts = await _apiService.GetContactsAsync(SearchText);

            try
            {
                Contacts.Clear();
                if (contacts != null)
                {
                    foreach (var c in contacts)
                        Contacts.Add(c);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", $"Ошибка обновления списка: {ex.Message}", "ОК");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void SearchContact()
        {
            var text = SearchText?.ToLower() ?? string.Empty;

            var filtered = Contacts
                .Where(c =>
                    c.FirstName.ToLower().Contains(text) ||
                    c.LastName.ToLower().Contains(text) ||
                    c.Email.ToLower().Contains(text) ||
                    c.Phone.Contains(text) ||
                    (c.Address != null && c.Address.ToLower().Contains(text)))
                .ToList();

            Contacts.Clear();
            foreach (var c in filtered)
                Contacts.Add(c);
        }


        [RelayCommand]
        private void CloseModal()
        {
            IsModalVisible = false;
            EditingContact = null;
        }

    }
}
