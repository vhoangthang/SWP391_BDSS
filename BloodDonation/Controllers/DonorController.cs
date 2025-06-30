// Controllers/DonorController.cs
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Models;
using BloodDonation.Repositories.Interfaces;

public class DonorController : Controller
{
    private readonly IDonorRepository _donorRepo;

    public DonorController(IDonorRepository donorRepo)
    {
        _donorRepo = donorRepo;
    }

    // GET: /Donor/
    public async Task<IActionResult> Index()
    {
        var donors = await _donorRepo.GetAllAsync();
        return View(donors);
    }

    // GET: /Donor/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var donor = await _donorRepo.GetByIdAsync(id);
        if (donor == null) return NotFound();
        return View(donor);
    }

    // GET: /Donor/Create
    public IActionResult Create() => View();

    // POST: /Donor/Create
    [HttpPost]
    public async Task<IActionResult> Create(Donor donor)
    {
        if (ModelState.IsValid)
        {
            await _donorRepo.AddAsync(donor);
            return RedirectToAction(nameof(Index));
        }
        return View(donor);
    }

    // GET: /Donor/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var donor = await _donorRepo.GetByIdAsync(id);
        if (donor == null) return NotFound();
        return View(donor);
    }

    // POST: /Donor/Edit/5
    [HttpPost]
    public async Task<IActionResult> Edit(Donor donor)
    {
        if (ModelState.IsValid)
        {
            await _donorRepo.UpdateAsync(donor);
            return RedirectToAction(nameof(Index));
        }
        return View(donor);
    }

    // GET: /Donor/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var donor = await _donorRepo.GetByIdAsync(id);
        if (donor == null) return NotFound();
        return View(donor);
    }

    // POST: /Donor/DeleteConfirmed/5
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _donorRepo.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
